using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.SharedQueryHost;
using ExampleProgram = RonSijm.MediumAPI.EndpointDocumentationExample.Program;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest;

public sealed class SharedQueryTests
{
    private static readonly string[] QueryNames = { "search", "customerId", "platform", "action", "entityType", "userId", "from", "to", "page", "pageSize", "projectId", "kind" };
    private static readonly Type[] QueryTypes = { typeof(string), typeof(long?), typeof(bool?), typeof(string), typeof(string), typeof(long?), typeof(string), typeof(string), typeof(int?), typeof(int?), typeof(long?), typeof(string) };

    [Fact]
    public async Task Complete_routes_API_Explorer_and_both_OpenAPI_documents_preserve_every_query_contract()
    {
        await using var before = Create("before");
        await before.StartAsync();
        foreach (var reference in new[] { "native", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            Equivalent(MediumApiContractComparison.Compare(before.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
            Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(before), Descriptions(app)));
            foreach (var document in new[] { DocumentationGroups.Public, DocumentationGroups.Admin })
            {
                Equivalent(MediumApiContractComparison.CompareOpenApi(ExampleProgram.GetOpenApiJson(before.Services, document), ExampleProgram.GetOpenApiJson(app.Services, document)));
            }

            var operations = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items).ToArray();
            Assert.Equal(2, operations.Length);
            foreach (var operation in operations)
            {
                Assert.Equal(QueryNames, operation.ParameterDescriptions.Select(parameter => parameter.Name));
                Assert.Equal(QueryTypes, operation.ParameterDescriptions.Select(parameter => parameter.Type));
                Assert.All(operation.ParameterDescriptions, parameter =>
                {
                    Assert.Equal("Query", parameter.Source?.Id);
                    Assert.False(parameter.IsRequired);
                    Assert.Null(parameter.DefaultValue);
                });
            }

            var json = JsonNode.Parse(ExampleProgram.GetOpenApiJson(app.Services, DocumentationGroups.Public))!;
            foreach (var path in Paths())
            {
                var parameters = Assert.IsType<JsonArray>(json["paths"]![path]!["get"]!["parameters"]);
                Assert.Equal(QueryNames, parameters.Select(parameter => parameter!["name"]!.GetValue<string>()));
                for (var index = 0; index < parameters.Count; index++)
                {
                    var parameter = Assert.IsType<JsonObject>(parameters[index]);
                    var schema = Assert.IsType<JsonObject>(parameter["schema"]);
                    Assert.Equal("query", parameter["in"]!.GetValue<string>());
                    Assert.False(parameter["required"]?.GetValue<bool>() ?? false);
                    Assert.False(schema.ContainsKey("default"));
                    Assert.Equal(index is 1 or 5 or 8 or 9 or 10 ? "integer" : index == 2 ? "boolean" : "string", schema["type"]!.GetValue<string>());
                    Assert.Equal(index is 1 or 5 or 10 ? "int64" : index is 8 or 9 ? "int32" : null, schema["format"]?.GetValue<string>());
                }
            }

            Assert.Empty(Probe(app).Calls);
            Assert.Empty(Probe(app).HandlerTokens);
        }
    }

    [Fact]
    public async Task Native_aggregation_deliberately_changes_only_the_CLR_argument_vector_and_keeps_explicit_optional_query_members()
    {
        var properties = typeof(SharedAuditQuery).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        Assert.Equal(QueryNames, properties.Select(property => property.Name));
        Assert.Equal(QueryTypes, properties.Select(property => property.PropertyType));
        Assert.NotNull(typeof(SharedAuditQuery).GetConstructor(Type.EmptyTypes));
        var empty = new SharedAuditQuery();
        var nullability = new NullabilityInfoContext();
        for (var index = 0; index < properties.Length; index++)
        {
            var property = properties[index];
            Assert.True(property.CanRead && property.CanWrite);
            Assert.Equal(QueryNames[index], property.GetCustomAttribute<FromQueryAttribute>()!.Name);
            Assert.Null(property.GetValue(empty));
            if (property.PropertyType == typeof(string)) { Assert.Equal(NullabilityState.Nullable, nullability.Create(property).ReadState); }
        }

        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            foreach (var path in Paths())
            {
                var parameters = Route(app, path).Metadata.GetMetadata<MethodInfo>()!.GetParameters();
                Assert.All(parameters, parameter => Assert.False(parameter.HasDefaultValue));
                Assert.Equal(typeof(SharedQueryService), parameters[^2].ParameterType);
                Assert.Equal(typeof(CancellationToken), parameters[^1].ParameterType);
                if (reference == "before")
                {
                    Assert.Equal(14, parameters.Length);
                    Assert.Equal(QueryNames, parameters.Take(12).Select(parameter => parameter.Name));
                    Assert.Equal(QueryTypes, parameters.Take(12).Select(parameter => parameter.ParameterType));
                }
                else
                {
                    Assert.Equal(3, parameters.Length);
                    Assert.Equal(typeof(SharedAuditQuery), parameters[0].ParameterType);
                    Assert.NotNull(parameters[0].GetCustomAttribute<AsParametersAttribute>());
                }
            }
        }
    }

    [Theory]
    [InlineData("missing", "")]
    [InlineData("empty", "?search=&action=&entityType=&from=&to=&kind=")]
    [InlineData("complete", "?search=needle&customerId=9&platform=true&action=create&entityType=project&userId=17&from=2026-11-01&to=2026-11-21&page=3&pageSize=25&projectId=41&kind=security")]
    [InlineData("invalid-dates", "?from=2026-1-2&to=2026-02-30")]
    [InlineData("multiple-strings", "?search=first&search=second&action=a&action=b&kind=c&kind=d")]
    [InlineData("case", "?SEARCH=needle&CUSTOMERID=9&PLATFORM=TrUe&USERID=17&PAGE=3&PAGESIZE=25&PROJECTID=41")]
    [InlineData("whitespace", "?search=++&action=+edit+&customerId=+9+&platform=+true+&page=+3+")]
    public async Task Missing_empty_case_multiple_string_and_date_inputs_keep_the_original_normalization_and_HTTP_contract(string scenario, string query)
    {
        await using var before = Create("before");
        await using var native = Create("native");
        await using var generated = Create();
        await before.StartAsync();
        await native.StartAsync();
        await generated.StartAsync();
        using var beforeClient = before.GetTestClient();
        using var nativeClient = native.GetTestClient();
        using var generatedClient = generated.GetTestClient();
        foreach (var path in Paths())
        {
            using var expected = await beforeClient.GetAsync(path + query);
            using var independent = await nativeClient.GetAsync(path + query);
            using var actual = await generatedClient.GetAsync(path + query);
            await SameResponse(expected, independent);
            await SameResponse(expected, actual);
            Assert.Equal(HttpStatusCode.OK, actual.StatusCode);
            var reply = (await actual.Content.ReadFromJsonAsync<SharedQueryReply>())!;
            Assert.Equal(path.EndsWith("/workspace", StringComparison.Ordinal) ? "workspace" : "search", reply.Kind);
            Assert.Equal(Expected(scenario), reply.Query);
            foreach (var app in new[] { before, native, generated })
            {
                var call = Probe(app).Calls.Last();
                Assert.Equal(reply.Kind, call.Kind);
                Assert.Equal(reply.Query, call.Query);
                Assert.Equal(Probe(app).RequestToken!.Value, call.Token);
                Assert.Equal(call.Token, Probe(app).HandlerTokens.Last());
                var arguments = Probe(app).Invocations.Last();
                Assert.Equal(ReferenceEquals(app, before) ? 14 : 3, arguments.Length);
                Assert.Same(app.Services.GetRequiredService<SharedQueryService>(), arguments[^2]);
                Assert.Equal(call.Token, Assert.IsType<CancellationToken>(arguments[^1]));
                if (!ReferenceEquals(app, before)) { Assert.IsType<SharedAuditQuery>(arguments[0]); }
            }

            Assert.Equal(Probe(before).Events, Probe(native).Events);
            Assert.Equal(Probe(before).Events, Probe(generated).Events);
        }

        Assert.Equal(2, Probe(generated).Calls.Count);
    }

    [Theory]
    [InlineData("customerId=invalid")]
    [InlineData("customerId=9223372036854775808")]
    [InlineData("platform=1")]
    [InlineData("userId=invalid")]
    [InlineData("page=2147483648")]
    [InlineData("pageSize=invalid")]
    [InlineData("projectId=invalid")]
    [InlineData("page=1&page=2")]
    [InlineData("customerId=")]
    [InlineData("platform=")]
    [InlineData("userId=")]
    [InlineData("page=")]
    [InlineData("pageSize=")]
    [InlineData("projectId=")]
    public async Task Every_native_scalar_parse_failure_keeps_false_mode_400_and_true_mode_exact_exception_before_handler_or_service(string query)
    {
        foreach (var throws in new[] { false, true })
        {
            await using var before = Create("before", throwOnBadRequest: throws);
            await before.StartAsync();
            using var beforeClient = before.GetTestClient();
            foreach (var reference in new[] { "native", "generated" })
            {
                await using var app = Create(reference, throwOnBadRequest: throws);
                await app.StartAsync();
                using var client = app.GetTestClient();
                foreach (var path in Paths())
                {
                    var beforeEvents = Probe(before).Events.Count;
                    var actualEvents = Probe(app).Events.Count;
                    if (throws)
                    {
                        var expected = await Failure(beforeClient, path + "?" + query);
                        var actual = await Failure(client, path + "?" + query);
                        Assert.Equal(expected.GetType(), actual.GetType());
                        Assert.Equal(expected.Message, actual.Message);
                        Assert.Equal(expected.StatusCode, actual.StatusCode);
                    }
                    else
                    {
                        using var expected = await beforeClient.GetAsync(path + "?" + query);
                        using var actual = await client.GetAsync(path + "?" + query);
                        await SameResponse(expected, actual);
                        Assert.Equal(HttpStatusCode.BadRequest, actual.StatusCode);
                    }

                    Assert.Equal(Probe(before).Events.Skip(beforeEvents), Probe(app).Events.Skip(actualEvents));
                }

                Assert.Empty(Probe(app).Calls);
                Assert.Empty(Probe(app).HandlerTokens);
            }

            Assert.Empty(Probe(before).Calls);
            Assert.Empty(Probe(before).HandlerTokens);
        }
    }

    [Fact]
    public async Task The_first_invalid_member_follows_native_declaration_order_even_when_the_URL_order_differs()
    {
        await using var before = Create("before", throwOnBadRequest: true);
        await before.StartAsync();
        using var beforeClient = before.GetTestClient();
        var query = "?page=bad&projectId=bad&platform=bad&customerId=bad";
        var expected = await Failure(beforeClient, Paths()[0] + query);
        Assert.Contains("customerId", expected.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("page", expected.Message, StringComparison.Ordinal);
        foreach (var reference in new[] { "native", "generated" })
        {
            await using var app = Create(reference, throwOnBadRequest: true);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var actual = await Failure(client, Paths()[0] + query);
            Assert.Equal(expected.GetType(), actual.GetType());
            Assert.Equal(expected.Message, actual.Message);
            Assert.Equal(expected.StatusCode, actual.StatusCode);
            Assert.Empty(Probe(app).Calls);
            Assert.Empty(Probe(app).HandlerTokens);
        }
    }

    [Fact]
    public async Task Query_aliases_do_not_take_values_from_headers_JSON_or_similarly_named_query_keys()
    {
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, Paths()[0] + "?customerId=7&customer_id=99&unscopedOnly=true&fromDate=2026-11-01");
            request.Headers.Add("search", "header-shadow");
            request.Headers.Add("customerId", "999");
            request.Content = new StringContent("{\"search\":\"body-shadow\",\"customerId\":555}", Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var reply = (await response.Content.ReadFromJsonAsync<SharedQueryReply>())!;
            Assert.Equal(Expected("missing") with { CustomerId = 7 }, reply.Query);
            Assert.Equal(reply.Query, Assert.Single(Probe(app).Calls).Query);
        }
    }

    [Fact]
    public async Task Application_validation_stays_in_the_existing_filter_after_binding_handler_and_service_entry()
    {
        await using var before = Create("before");
        await before.StartAsync();
        using var beforeClient = before.GetTestClient();
        using var expected = await beforeClient.GetAsync(Paths()[0] + "?search=validation");
        foreach (var reference in new[] { "native", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(Paths()[0] + "?search=validation");
            await SameResponse(expected, response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            Assert.Equal("validation", Assert.Single(Probe(app).Calls).Query.Search);
            Assert.Single(Probe(app).HandlerTokens);
            Assert.Equal(Probe(before).Events, Probe(app).Events);
            Assert.Equal(new[] { "filter-enter", "handler", "service:search", "translated" }, Probe(app).Events);
        }
    }

    [Fact]
    public async Task Named_aliases_reuse_binding_and_keep_independent_host_service_state()
    {
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var first = Create(reference, alias: true);
            await using var second = Create(reference);
            await first.StartAsync();
            await second.StartAsync();
            Assert.Equal("SharedAuditAlias", Route(first, "/query-sample/alias").Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
            Assert.NotSame(Probe(first), Probe(second));
            using var client = first.GetTestClient();
            using var response = await client.GetAsync("/query-sample/alias?search=alias&page=2");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var reply = (await response.Content.ReadFromJsonAsync<SharedQueryReply>())!;
            Assert.Equal("search", reply.Kind);
            Assert.Equal(Expected("missing") with { Search = "alias", Page = 2 }, reply.Query);
            Assert.Single(Probe(first).Calls);
            Assert.Empty(Probe(second).Calls);
            Assert.DoesNotContain(Routes(second), route => route.RoutePattern.RawText == "/query-sample/alias");
        }
    }

    [Fact]
    public async Task Public_sample_selection_keeps_default_routes_and_disabled_namespaces_independent()
    {
        await using var selected = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration["Sample:SharedQueries"] = "true";
        });
        await using var original = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await using var disabled = Create(enabled: false, alias: true);
        await selected.StartAsync();
        await original.StartAsync();
        await disabled.StartAsync();
        Assert.Equal(Paths(), Routes(selected).Select(route => route.RoutePattern.RawText).OrderBy(path => path, StringComparer.Ordinal));
        Assert.DoesNotContain(Routes(original), route => route.RoutePattern.RawText!.StartsWith("/query-sample", StringComparison.Ordinal));
        Assert.Empty(Routes(disabled));
        Assert.Empty(Probe(disabled).Calls);
        using var client = selected.GetTestClient();
        using var response = await client.GetAsync(Paths()[1]);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("workspace", (await response.Content.ReadFromJsonAsync<SharedQueryReply>())!.Kind);
    }

    private static string[] Paths()
    {
        return new[] { "/query-sample/audit-logs", "/query-sample/workspace" };
    }

    private static SharedQueryInput Expected(string scenario)
    {
        var empty = new SharedQueryInput("", null, false, null, null, null, null, null, 1, 10, null, null);
        return scenario switch
        {
            "missing" or "invalid-dates" => empty,
            "empty" => empty with { Action = "", EntityType = "", Kind = "" },
            "complete" => new SharedQueryInput("needle", 9, true, "create", "project", 17, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 21), 3, 25, 41, "security"),
            "multiple-strings" => empty with { Search = "first,second", Action = "a,b", Kind = "c,d" },
            "case" => empty with { Search = "needle", CustomerId = 9, UnscopedOnly = true, UserId = 17, Page = 3, PageSize = 25, ProjectId = 41 },
            "whitespace" => empty with { Search = "  ", CustomerId = 9, UnscopedOnly = true, Action = " edit ", Page = 3 },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
    }

    private static async Task<BadHttpRequestException> Failure(HttpClient client, string path)
    {
        return await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
        {
            using var response = await client.GetAsync(path);
        });
    }

    private static async Task SameResponse(HttpResponseMessage expected, HttpResponseMessage actual)
    {
        Assert.Equal(expected.StatusCode, actual.StatusCode);
        Assert.Equal(expected.Content.Headers.ContentType?.ToString(), actual.Content.Headers.ContentType?.ToString());
        Assert.Equal(await expected.Content.ReadAsByteArrayAsync(), await actual.Content.ReadAsByteArrayAsync());
    }

    private static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    }
}