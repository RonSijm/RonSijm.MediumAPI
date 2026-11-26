using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.BindingMetadataExample.Program;

namespace RonSijm.MediumAPI.BindingMetadataExample.IntegrationTest;

public sealed class NamedHeaderMigrationTests
{
    [Fact]
    public async Task Native_header_alias_keeps_all_three_metadata_interfaces_nullable_CLR_slots_and_complete_contracts()
    {
        await using var app = CreateHeaders();
        await app.StartAsync();
        foreach (var reference in new[] { "before", "native" })
        {
            await using var expected = CreateHeaders(reference);
            await expected.StartAsync();
            Equivalent(MediumApiContractComparison.Compare(expected.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
            Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(expected), Descriptions(app)));
            foreach (var route in Routes(app))
            {
                var expectedRoute = Route(expected, route.RoutePattern.RawText!);
                var actualParameters = route.Metadata.GetMetadata<MethodInfo>()!.GetParameters();
                Assert.Equal(Signatures(expectedRoute), Signatures(route));
                var parameter = Assert.Single(actualParameters, parameter => parameter.Name == "idempotencyKey");
                Assert.Equal(typeof(string), parameter.ParameterType);
                Assert.Equal(NullabilityState.Nullable, new NullabilityInfoContext().Create(parameter).ReadState);
                Assert.False(parameter.HasDefaultValue);
                Assert.False(parameter.IsOptional);
                var alias = Assert.IsType<CommandKeyAttribute>(Assert.Single(parameter.GetCustomAttributes<FromHeaderAttribute>()));
                Assert.Equal(HeaderBindingMetadata.HeaderName, ((IFromHeaderMetadata)alias).Name);
                Assert.Equal(HeaderBindingMetadata.HeaderName, ((IModelNameProvider)alias).Name);
                Assert.Same(BindingSource.Header, ((IBindingSourceMetadata)alias).BindingSource);
            }
        }

        foreach (var operation in Descriptions(app).Operations)
        {
            var header = Assert.Single(operation.Parameters, parameter => parameter.Name == HeaderBindingMetadata.HeaderName);
            Assert.Equal("Header", header.Source);
            Assert.False(header.IsRequired);
            Assert.Equal(typeof(string).FullName, header.Type);
        }

        var mutable = new CommandKeyAttribute();
        mutable.Name = "X-Another-Header";
        Assert.Equal("X-Another-Header", ((IFromHeaderMetadata)mutable).Name);
        Assert.Equal("X-Another-Header", ((IModelNameProvider)mutable).Name);
    }

    [Theory]
    [InlineData("absent")]
    [InlineData("empty")]
    [InlineData("whitespace")]
    [InlineData("single")]
    [InlineData("multiple")]
    [InlineData("comma")]
    [InlineData("lowercase")]
    [InlineData("uppercase")]
    public async Task Optional_values_wire_casing_and_multiple_values_keep_native_HTTP_and_bound_service_values(string mode)
    {
        foreach (var manual in new[] { false, true })
        {
            byte[]? expectedBody = null;
            string? expectedKey = null;
            string? expectedMedia = null;
            foreach (var reference in new[] { "before", "native", "generated" })
            {
                await using var app = CreateHeaders(reference);
                await app.StartAsync();
                using var client = app.GetTestClient();
                using var request = CommandRequest(manual);
                AddHeader(request, mode);
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                Assert.Equal(HeaderBindingMetadata.Location, response.Headers.Location?.OriginalString);
                Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
                var call = Assert.Single(Probe(app).ServiceCalls);
                var body = await response.Content.ReadAsByteArrayAsync();
                var media = response.Content.Headers.ContentType?.ToString();
                if (expectedBody is null)
                {
                    expectedBody = body;
                    expectedKey = call.Key;
                    expectedMedia = media;
                }
                else
                {
                    Assert.Equal(expectedBody, body);
                    Assert.Equal(expectedKey, call.Key);
                    Assert.Equal(expectedMedia, media);
                }

                if (mode == "absent") { Assert.Null(call.Key); }

                if (mode is "single" or "lowercase" or "uppercase") { Assert.Equal("synthetic-key", call.Key); }

                Assert.Equal(new HeaderFactoryVisit(manual ? 2 : 1, 5, call.Key), Assert.Single(Probe(app).FactoryVisits));
                Assert.Equal(1, Probe(app).Entries);
                Assert.Equal(manual ? 1 : 0, Probe(app).FormReads);
            }
        }
    }

    [Fact]
    public async Task Query_values_do_not_fill_an_absent_header_or_replace_an_explicit_header_value()
    {
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreateHeaders(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var withHeader in new[] { false, true })
            {
                using var request = CommandRequest(false);
                request.RequestUri = new Uri("/header-commands/json?idempotencyKey=query&Idempotency-Key=query", UriKind.Relative);
                if (withHeader) { AddHeader(request, "single"); }

                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                Assert.Equal(withHeader ? "synthetic-key" : null, Probe(app).ServiceCalls.Last().Key);
            }
        }
    }

    [Fact]
    public async Task Original_body_guard_factory_uses_the_same_slots_and_stops_before_handler_owned_headers_and_services()
    {
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreateHeaders(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = CommandRequest(false);
            request.Content = JsonContent.Create(new HeaderCommand(""));
            AddHeader(request, "single");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Null(response.Headers.CacheControl);
            Assert.Empty(await response.Content.ReadAsByteArrayAsync());
            Assert.Equal(new HeaderFactoryVisit(1, 5, "synthetic-key"), Assert.Single(Probe(app).FactoryVisits));
            Assert.Equal(0, Probe(app).Entries);
            Assert.Empty(Probe(app).ServiceCalls);
        }
    }

    [Fact]
    public async Task Malformed_JSON_preserves_native_failure_and_factory_boundaries_without_handler_entry()
    {
        HeaderFactoryVisit[]? expectedVisits = null;
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreateHeaders(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = CommandRequest(false);
            request.Content = new StringContent("{ synthetic-invalid-json", Encoding.UTF8, "application/json");
            AddHeader(request, "single");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Null(response.Headers.CacheControl);
            if (expectedVisits is null) { expectedVisits = Probe(app).FactoryVisits.ToArray(); }
            else { Assert.Equal(expectedVisits, Probe(app).FactoryVisits); }

            Assert.Equal(0, Probe(app).Entries);
            Assert.Equal(0, Probe(app).FormReads);
            Assert.Empty(Probe(app).ServiceCalls);
        }
    }

    [Fact]
    public async Task Manual_body_reading_and_handler_owned_non_form_errors_keep_their_existing_order_and_header()
    {
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreateHeaders(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = CommandRequest(true);
            request.Content = JsonContent.Create(new HeaderCommand("synthetic-command"));
            AddHeader(request, "single");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
            Assert.Equal(new HeaderFactoryVisit(2, 5, "synthetic-key"), Assert.Single(Probe(app).FactoryVisits));
            Assert.Equal(1, Probe(app).Entries);
            Assert.Equal(0, Probe(app).FormReads);
            Assert.Empty(Probe(app).ServiceCalls);
        }
    }

    [Fact]
    public async Task Public_flag_selects_two_commands_and_keeps_the_original_four_route_example()
    {
        await using var selected = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration["Sample:NamedHeaderBindings"] = "true";
        });
        await using var original = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await selected.StartAsync();
        await original.StartAsync();
        Assert.Equal(2, Routes(selected).Count());
        Assert.Equal(4, Routes(original).Count());
        Assert.DoesNotContain(Routes(original), route => route.RoutePattern.RawText!.Contains("header-commands", StringComparison.Ordinal));
        using var client = selected.GetTestClient();
        using var request = CommandRequest(false);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static WebApplication CreateHeaders(string reference = "generated")
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Configuration["Sample:NamedHeaderBindings"] = "true";
        }, mapEndpoints: false);
        if (reference == "generated") { NamedHeaderScenario.Map(app); }
        else
        {
            var group = app.MapGroup("/header-commands");
            AddNativeFactory(MapOriginal(group, "/json", reference == "before" ? (Delegate)BeforeJson : NativeJson)).WithName("PostHeaderJson");
            AddNativeFactory(MapOriginal(group, "/{customerId:long}/manual", reference == "before" ? (Delegate)BeforeManual : NativeManual)).WithName("PostHeaderManual");
        }

        return app;
    }

    private static RouteHandlerBuilder AddNativeFactory(RouteHandlerBuilder endpoint)
    {
        endpoint.WithMetadata(new ProducesResponseTypeMetadata(400, typeof(void), Array.Empty<string>()));
        return endpoint.AddEndpointFilterFactory((context, next) =>
        {
            var parameters = context.MethodInfo.GetParameters();
            var keyIndex = parameters.Single(parameter => parameter.ParameterType == typeof(string) && parameter.Name == "idempotencyKey").Position;
            var bodyIndex = parameters.SingleOrDefault(parameter => parameter.ParameterType == typeof(HeaderCommand))?.Position;
            return invocation =>
            {
                var key = invocation.GetArgument<string?>(keyIndex);
                invocation.HttpContext.RequestServices.GetRequiredService<HeaderBindingProbe>().RecordFactory(keyIndex, invocation.Arguments.Count, key);
                if (bodyIndex is int index && string.IsNullOrWhiteSpace(invocation.GetArgument<HeaderCommand?>(index)?.Name)) { return ValueTask.FromResult<object?>(TypedResults.BadRequest()); }

                return next(invocation);
            };
        });
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapMethods(pattern, new[] { HttpMethods.Post }, handler);
    }

    [Tags(HeaderBindingMetadata.Tag)]
    private static async Task<Created<HeaderReceipt>> BeforeJson([FromBody] HeaderCommand request, [FromHeader(Name = HeaderBindingMetadata.HeaderName)] string? idempotencyKey, HttpContext context, [FromServices] HeaderCommandService service, CancellationToken token)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.RequestServices.GetRequiredService<HeaderBindingProbe>().RecordEntry();
        return TypedResults.Created(HeaderBindingMetadata.Location, await service.CreateAsync(request.Name, idempotencyKey, "json", token));
    }

    [Tags(HeaderBindingMetadata.Tag)]
    private static async Task<Created<HeaderReceipt>> NativeJson([FromBody] HeaderCommand request, [CommandKey] string? idempotencyKey, HttpContext context, [FromServices] HeaderCommandService service, CancellationToken token)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.RequestServices.GetRequiredService<HeaderBindingProbe>().RecordEntry();
        return TypedResults.Created(HeaderBindingMetadata.Location, await service.CreateAsync(request.Name, idempotencyKey, "json", token));
    }

    [Tags(HeaderBindingMetadata.Tag)]
    private static async Task<Results<Created<HeaderReceipt>, BadRequest>> BeforeManual([FromRoute] long customerId, HttpContext context, [FromHeader(Name = HeaderBindingMetadata.HeaderName)] string? idempotencyKey, [FromServices] HeaderCommandService service, CancellationToken token)
    {
        context.Response.Headers.CacheControl = "no-store";
        var probe = context.RequestServices.GetRequiredService<HeaderBindingProbe>();
        probe.RecordEntry();
        if (!context.Request.HasFormContentType) { return TypedResults.BadRequest(); }

        probe.RecordEntry(readForm: true);
        var form = await context.Request.ReadFormAsync(token);
        return TypedResults.Created(HeaderBindingMetadata.Location, await service.CreateAsync(form["name"].ToString(), idempotencyKey, "manual", token));
    }

    [Tags(HeaderBindingMetadata.Tag)]
    private static async Task<Results<Created<HeaderReceipt>, BadRequest>> NativeManual([FromRoute] long customerId, HttpContext context, [CommandKey] string? idempotencyKey, [FromServices] HeaderCommandService service, CancellationToken token)
    {
        context.Response.Headers.CacheControl = "no-store";
        var probe = context.RequestServices.GetRequiredService<HeaderBindingProbe>();
        probe.RecordEntry();
        if (!context.Request.HasFormContentType) { return TypedResults.BadRequest(); }

        probe.RecordEntry(readForm: true);
        var form = await context.Request.ReadFormAsync(token);
        return TypedResults.Created(HeaderBindingMetadata.Location, await service.CreateAsync(form["name"].ToString(), idempotencyKey, "manual", token));
    }

    private static HttpRequestMessage CommandRequest(bool manual)
    {
        return new HttpRequestMessage(HttpMethod.Post, manual ? "/header-commands/7/manual" : "/header-commands/json") { Content = manual ? new FormUrlEncodedContent(new Dictionary<string, string> { ["name"] = "synthetic-command" }) : JsonContent.Create(new HeaderCommand("synthetic-command")) };
    }

    private static void AddHeader(HttpRequestMessage request, string mode)
    {
        if (mode == "absent") { return; }

        var name = mode switch { "lowercase" => HeaderBindingMetadata.HeaderName.ToLowerInvariant(), "uppercase" => HeaderBindingMetadata.HeaderName.ToUpperInvariant(), _ => HeaderBindingMetadata.HeaderName };
        var values = mode switch { "empty" => new[] { "" }, "whitespace" => new[] { "  " }, "multiple" => new[] { "first", "second" }, "comma" => new[] { "first,second" }, _ => new[] { "synthetic-key" } };
        Assert.True(request.Headers.TryAddWithoutValidation(name, values));
    }

    private static IEnumerable<RouteEndpoint> Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    }

    private static RouteEndpoint Route(WebApplication app, string path)
    {
        return Assert.Single(Routes(app), route => route.RoutePattern.RawText == path);
    }

    private static HeaderBindingProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<HeaderBindingProbe>();
    }

    private static MediumApiDescriptionSnapshot Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    }

    private static string[] Signatures(RouteEndpoint route)
    {
        var nullability = new NullabilityInfoContext();
        return route.Metadata.GetMetadata<MethodInfo>()!.GetParameters().Select(parameter => string.Join("|", parameter.Position, parameter.Name, parameter.ParameterType.AssemblyQualifiedName, nullability.Create(parameter).ReadState, parameter.HasDefaultValue, parameter.IsOptional)).ToArray();
    }

    private static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    }
}