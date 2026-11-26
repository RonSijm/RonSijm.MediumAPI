using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.SharedResponsesExample.Endpoints.Api;
using Xunit;
using static RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.SharedResponsesHost;

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest;

public sealed class NativeSharedResponsesTests
{
    [Fact]
    public async Task Class_group_and_filter_response_values_match_independent_native_metadata_and_API_contracts()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        await app.StartAsync();
        await native.StartAsync();
        Assert.Equal(8, Routes(app).Count());
        Assert.Equal(MetadataInventory(native), MetadataInventory(app));
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(app)));
        Assert.Equal(0, Probe(app).Activations);
        Assert.Equal(0, Probe(app).TotalCalls);
        Assert.Equal(0, Probe(native).TotalCalls);
    }

    [Fact]
    public async Task Complete_real_provider_documents_match_without_stripping_schemas_or_overlapping_response_statuses()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        await app.StartAsync();
        await native.StartAsync();
        var comparison = MediumApiContractComparison.CompareOpenApi(OpenApiJson(native), OpenApiJson(app));
        Equivalent(comparison);
        comparison.ThrowIfDifferent();
        Assert.Equal(0, Probe(app).Activations);
        Assert.Equal(0, Probe(app).TotalCalls);
    }

    [Fact]
    public async Task Provider_documents_have_real_typed_success_filter_errors_group_responses_and_explicit_alias_boundaries()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var document = OpenApi(app);
            Assert.StartsWith("3.", document["openapi"]!.GetValue<string>(), StringComparison.Ordinal);
            Assert.Equal(8, Assert.IsType<JsonObject>(document["paths"]).Count);
            var plain = Operation(document, "/responses/plain");
            Assert.Equal("GetPlain", plain["operationId"]!.GetValue<string>());
            Assert.Equal("#/components/schemas/SharedReply", plain["responses"]!["200"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>());
            Assert.NotNull(plain["responses"]!["503"]);
            var guarded = Operation(document, "/responses/guarded/{value}");
            Assert.Equal("#/components/schemas/FilterError", guarded["responses"]!["400"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>());
            var conventional = Operation(document, "/responses/conventional/{value}");
            Assert.NotNull(conventional["responses"]!["409"]!["content"]![SharedResponseMetadata.ProblemJson]);
            Assert.NotNull(conventional["responses"]!["429"]);
            var alias = Operation(document, "/aliases/manual/{value}");
            Assert.Equal("ManualSharedAlias", alias["operationId"]!.GetValue<string>());
            Assert.NotNull(alias["responses"]!["410"]);
            Assert.NotNull(alias["responses"]!["400"]);
            Assert.Null(alias["responses"]!["503"]);
            var schemas = Assert.IsType<JsonObject>(document["components"]!["schemas"]);
            Assert.True(schemas.ContainsKey(nameof(SharedReply)));
            Assert.True(schemas.ContainsKey(nameof(FilterError)));
        }
    }

    [Theory]
    [InlineData("/responses/plain", "plain", 0)]
    [InlineData("/responses/documented", "documented", 0)]
    [InlineData("/responses/method", "method", 0)]
    [InlineData("/responses/guarded/17", "guarded", 17)]
    [InlineData("/responses/conventional/17", "conventional", 17)]
    [InlineData("/responses/overlap", "overlap", 0)]
    [InlineData("/fluent/value/17", "fluent", 17)]
    [InlineData("/aliases/manual/17", "manual", 17)]
    public async Task Healthy_HTTP_results_remain_identical_to_native_with_no_response_wrapper_or_runtime_status_rewrite(string path, string source, int value)
    {
        string? expectedBody = null;
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync();
            if (native)
            {
                expectedBody = body;
            }
            else
            {
                Assert.Equal(expectedBody, body);
            }

            Assert.Equal(new SharedReply(source, value), await response.Content.ReadFromJsonAsync<SharedReply>());
            Assert.Equal(1, Probe(app).Calls(source));
        }
    }

    [Theory]
    [InlineData("/responses/guarded/-1", "guarded")]
    [InlineData("/fluent/value/-1", "fluent")]
    [InlineData("/aliases/manual/-1", "manual")]
    public async Task Plain_native_filter_classes_own_400_metadata_and_short_circuit_without_calling_the_handler(string path, string source)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(new FilterError("negative", "Use a nonnegative value."), await response.Content.ReadFromJsonAsync<FilterError>());
            Assert.Equal(new[] { "negative" }, response.Headers.GetValues("X-Filter-Owner"));
            Assert.Equal(1, Probe(app).GuardVisits);
            Assert.Equal(0, Probe(app).Calls(source));
        }
    }

    [Fact]
    public async Task Convention_aware_filter_keeps_Configure_responses_before_its_declarations_and_returns_its_own_problem()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var responses = Route(app, "/responses/conventional/{value:int}").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
            Assert.Equal(new[] { 503, 200, 429, 409 }, responses.Select(response => response.StatusCode));
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/responses/conventional/13");
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(SharedResponseMetadata.ProblemJson, response.Content.Headers.ContentType?.MediaType);
            var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
            Assert.Equal(409, problem.Status);
            Assert.Equal("Choose another value.", problem.Title);
            Assert.Equal("urn:sample:special-value", problem.Type);
            Assert.Equal(1, Probe(app).ConventionalVisits);
            Assert.Equal(0, Probe(app).Calls("conventional"));
        }
    }

    [Fact]
    public async Task Declared_class_and_namespace_statuses_do_not_install_error_behavior_or_change_a_success_result()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            Assert.Equal(new[] { 503, 418, 200 }, Route(app, "/responses/documented").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(response => response.StatusCode));
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new SharedReply("documented", 0), await Read(client, "/responses/documented"));
            Assert.Equal(1, Probe(app).Calls("documented"));
        }
    }

    [Fact]
    public async Task Typed_success_is_declared_once_by_the_result_type_without_redundant_200_attributes()
    {
        await using var app = Create();
        foreach (var route in Routes(app))
        {
            var success = Assert.Single(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 200);
            Assert.Equal(typeof(SharedReply), success.Type);
            Assert.Equal(new[] { "application/json" }, success.ContentTypes);
            Assert.DoesNotContain(route.Metadata.GetOrderedMetadata<EndpointResponseAttribute>(), response => response.StatusCode == 200);
        }
    }

    [Fact]
    public async Task Same_status_declarations_remain_in_raw_order_while_API_Explorer_and_provider_output_follow_native_processing()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        var expected = new[] { typeof(EarlierError), typeof(MethodError), typeof(ConfiguredError) };
        Assert.Equal(expected, Route(app, "/responses/overlap").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Where(response => response.StatusCode == 404).Select(response => response.Type));
        Assert.Equal(expected, Route(native, "/responses/overlap").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Where(response => response.StatusCode == 404).Select(response => response.Type));
        await app.StartAsync();
        await native.StartAsync();
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(app)));
        var expectedResponse = Operation(OpenApi(native), "/responses/overlap")["responses"]!["404"]!.ToJsonString();
        var actualResponse = Operation(OpenApi(app), "/responses/overlap")["responses"]!["404"]!.ToJsonString();
        Equivalent(MediumApiContractComparison.CompareJson(expectedResponse, actualResponse));
    }

    [Fact]
    public async Task Method_and_class_declarations_retain_status_only_defaults_and_ordered_media_types()
    {
        await using var app = Create();
        var responses = Route(app, "/responses/method").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        Assert.Equal(new[] { 503, 422, 200, 401 }, responses.Select(response => response.StatusCode));
        var declared = Assert.Single(responses, response => response.StatusCode == 422);
        Assert.Equal(typeof(FilterError), declared.Type);
        Assert.Equal(new[] { "application/json", "text/plain" }, declared.ContentTypes);
        var empty = Assert.Single(responses, response => response.StatusCode == 401);
        Assert.Equal(typeof(void), empty.Type);
        Assert.Empty(empty.ContentTypes);
    }

    [Fact]
    public async Task An_explicit_alias_keeps_class_and_registered_filter_metadata_without_importing_the_namespace_owner()
    {
        await using var app = Create();
        var alias = Route(app, "/aliases/manual/{value:int}");
        Assert.Equal(new[] { 410, 200, 400 }, alias.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(response => response.StatusCode));
        Assert.Equal("ManualSharedAlias", alias.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
        Assert.DoesNotContain(Routes(app), route => route.RoutePattern.RawText == "/manual/{value:int}");
    }

    [Fact]
    public async Task Reports_API_descriptions_and_provider_documents_do_not_activate_owned_handler_targets()
    {
        await using var app = Create();
        Assert.False(app.GetMediumApiRouteReport().HasConflicts);
        Assert.Equal(8, app.GetMediumApiContractSnapshot().Routes.Count);
        await app.StartAsync();
        Assert.Equal(8, Descriptions(app).Operations.Count);
        _ = OpenApiJson(app);
        Assert.Equal(0, Probe(app).Activations);
        Assert.Equal(0, Probe(app).Disposals);
        Assert.Equal(0, Probe(app).TotalCalls);
    }

    [Fact]
    public async Task New_response_metadata_keeps_fresh_owned_targets_disposed_after_each_dispatch()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new SharedReply("plain", 0), await Read(client, "/responses/plain"));
        Assert.Equal(new SharedReply("plain", 0), await Read(client, "/responses/plain"));
        Assert.Equal(2, Probe(app).Activations);
        Assert.Equal(2, Probe(app).Disposals);
        Assert.Equal(2, Probe(app).Calls("plain"));
        var original = Route(app, "/responses/plain").Metadata.GetMetadata<MethodInfo>();
        Assert.Equal(typeof(GetPlainEndpoint), original?.DeclaringType);
        Assert.Equal(typeof(Ok<SharedReply>), original?.ReturnType);
    }

    [Fact]
    public async Task Native_route_constraints_reject_unmatched_values_without_running_filter_or_handler_code()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/responses/guarded/not-an-integer");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, Probe(app).GuardVisits);
            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }
}