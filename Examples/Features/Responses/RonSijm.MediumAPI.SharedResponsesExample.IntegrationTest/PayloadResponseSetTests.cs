using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.SharedResponsesExample.PayloadResponseSets;
using Xunit;
using static RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.SharedResponsesHost;
using ExampleProgram = RonSijm.MediumAPI.SharedResponsesExample.Program;

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest;

public sealed class PayloadResponseSetTests
{
    [Fact]
    public void Protected_constructor_snapshots_membership_without_cloning_children_subtypes_or_duplicate_references()
    {
        var problem = new ProblemResponseAttribute(400);
        var format = new FormatResponseAttribute("export", 418, typeof(string), "text/plain", "text/csv");
        var input = new List<EndpointResponseAttribute> { problem, format, problem };
        var set = new SuppliedResponsesAttribute(input);
        input[0] = new EndpointResponseAttribute(500);
        input.Clear();
        Assert.Equal(new[] { 400, 418, 400 }, set.Responses.Select(response => response.StatusCode));
        Assert.Same(problem, set.Responses[0]);
        Assert.Same(format, set.Responses[1]);
        Assert.Same(problem, set.Responses[2]);
        Assert.IsType<ProblemResponseAttribute>(set.Responses[0]);
        Assert.Equal("export", Assert.IsType<FormatResponseAttribute>(set.Responses[1]).Format);
        Assert.Equal(typeof(ProblemDetails), set.Responses[0].Type);
        Assert.Equal(new[] { SharedResponseMetadata.ProblemJson }, set.Responses[0].ContentTypes);
        Assert.Equal(new[] { "text/plain", "text/csv" }, set.Responses[1].ContentTypes);
        Assert.Throws<NotSupportedException>(() => ((IList<EndpointResponseAttribute>)set.Responses)[0] = new EndpointResponseAttribute(500));
    }

    [Fact]
    public void Protected_constructor_rejects_null_lists_and_null_children_with_the_response_parameter_name()
    {
        Assert.Equal("responses", Assert.Throws<ArgumentNullException>(() => new SuppliedResponsesAttribute(null!)).ParamName);
        Assert.Equal("responses", Assert.Throws<ArgumentException>(() => new SuppliedResponsesAttribute(new EndpointResponseAttribute[] { new(400), null! })).ParamName);
    }

    [Fact]
    public void Empty_payload_lists_are_valid_and_the_public_status_constructor_remains_the_only_public_overload()
    {
        Assert.Empty(new SuppliedResponsesAttribute(Array.Empty<EndpointResponseAttribute>()).Responses);
        Assert.Empty(new EmptyPayloadResponsesAttribute().Responses);
        var publicConstructor = Assert.Single(typeof(EndpointResponsesAttribute).GetConstructors());
        var parameter = Assert.Single(publicConstructor.GetParameters());
        Assert.Equal(typeof(int[]), parameter.ParameterType);
        Assert.True(parameter.IsDefined(typeof(ParamArrayAttribute)));
        Assert.Equal("statusCodes", Assert.Throws<ArgumentNullException>(() => new EndpointResponsesAttribute(null!)).ParamName);
        var payloadConstructor = Assert.Single(typeof(EndpointResponsesAttribute).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.True(payloadConstructor.IsFamily);
        Assert.Equal(typeof(IReadOnlyList<EndpointResponseAttribute>), Assert.Single(payloadConstructor.GetParameters()).ParameterType);
        Assert.False(Assert.Single(payloadConstructor.GetParameters()).IsDefined(typeof(ParamArrayAttribute)));
    }

    [Theory]
    [InlineData(7, 200)]
    [InlineData(-1, 400)]
    public async Task Shared_problem_payload_declarations_preserve_HTTP_success_native_problem_errors_and_bytes(int value, int status)
    {
        byte[]? expectedBody = null;
        string? expectedMedia = null;
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreatePayloads(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/payload-response-sets/value/" + value);
            Assert.Equal(status, (int)response.StatusCode);
            var body = await response.Content.ReadAsByteArrayAsync();
            var media = response.Content.Headers.ContentType?.ToString();
            if (expectedBody is null)
            {
                expectedBody = body;
                expectedMedia = media;
            }
            else
            {
                Assert.Equal(expectedBody, body);
                Assert.Equal(expectedMedia, media);
            }

            Assert.Equal(status == 200 ? 1 : 0, Probe(app).TotalCalls);
            Assert.Equal(1, Probe(app).GuardVisits);
        }
    }

    [Theory]
    [InlineData("/payload-response-sets/mixed", "payload-response-set-mixed", 0)]
    [InlineData("/payload-response-sets/empty", "payload-response-set-empty", 0)]
    [InlineData("/aliases/payload-response-sets/7", "payload-response-set", 7)]
    public async Task Mixed_empty_and_alias_declarations_keep_the_actual_typed_payloads(string path, string source, int value)
    {
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreatePayloads(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new SharedReply(source, value), await client.GetFromJsonAsync<SharedReply>(path));
        }
    }

    [Fact]
    public async Task Complete_ordered_runtime_payload_media_route_API_Explorer_and_OpenAPI_contracts_match_independent_references()
    {
        await using var app = CreatePayloads();
        await app.StartAsync();
        foreach (var reference in new[] { "before", "native" })
        {
            await using var expected = CreatePayloads(reference);
            await expected.StartAsync();
            Assert.Equal(MetadataInventory(expected), MetadataInventory(app));
            foreach (var route in Routes(expected))
            {
                var actual = Route(app, route.RoutePattern.RawText!);
                Assert.Equal(RuntimeResponses(route), RuntimeResponses(actual));
                Assert.Equal(Phases(route), Phases(actual));
            }

            Equivalent(MediumApiContractComparison.Compare(expected.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
            Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(expected), Descriptions(app)));
            Equivalent(MediumApiContractComparison.CompareOpenApi(OpenApiJson(expected), OpenApiJson(app)));
        }

        Assert.Equal(4, Routes(app).Count());
        Assert.Equal(0, Probe(app).TotalCalls);
        var value = Route(app, "/payload-response-sets/value/{value:int}");
        Assert.All(value.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Where(response => response.StatusCode is 400 or 409 or 410 or 422 or 429 or 431 or 452 or 453 or 502 or 503), response => Assert.IsType<ProblemResponseAttribute>(response));
        var formats = value.Metadata.GetOrderedMetadata<FormatResponseAttribute>();
        Assert.Equal(new[] { "text", "structured", "text" }, formats.Select(response => response.Format));
        Assert.Same(formats[0], formats[2]);
        Assert.All(Routes(app), route => Assert.Empty(route.Metadata.OfType<EndpointResponsesAttribute>()));
    }

    [Theory]
    [InlineData("endpoint")]
    [InlineData("group")]
    public async Task Combined_filter_and_factory_payload_sets_keep_native_configure_custom_and_response_positions(string scope)
    {
        await using var app = CreatePayloads(map: false);
        IEndpointConventionBuilder builder;
        RouteGroupBuilder? group = null;
        if (scope == "group")
        {
            group = app.MapGroup("");
            builder = group;
        }
        else { builder = app.MapGet("/phase/{value:int}", PhaseValue); }

        builder.AddMediumApiFilter<PayloadResponseFilter>().AddMediumApiFilterFactory<PayloadResponseFactory>();
        if (group is not null) { group.MapGet("/phase/{value:int}", PhaseValue); }

        await app.StartAsync();
        var route = Route(app, "/phase/{value:int}");
        Assert.Equal(new[] { "payload-filter-configure", "payload-filter-custom", "425", "429", "431", "432", "payload-factory-configure", "payload-factory-custom", "451", "452", "453" }, Phases(route).Where(phase => phase != "200"));
        foreach (var response in route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Where(response => response.StatusCode is 429 or 431 or 452 or 453))
        {
            Assert.IsType<ProblemResponseAttribute>(response);
            Assert.Equal(typeof(ProblemDetails), response.Type);
            Assert.Equal(new[] { SharedResponseMetadata.ProblemJson }, response.ContentTypes);
        }

        using var client = app.GetTestClient();
        Assert.Equal(new SharedReply("payload-phase", 7), await client.GetFromJsonAsync<SharedReply>("/phase/7"));
    }

    [Fact]
    public async Task Typed_MVC_fluent_final_media_and_fallback_contracts_keep_the_native_priority_and_declared_subtypes()
    {
        await using var app = CreatePayloads();
        await using var native = CreatePayloads("native");
        await app.StartAsync();
        await native.StartAsync();
        var value = Route(app, "/payload-response-sets/value/{value:int}").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        Assert.Equal(typeof(ProblemDetails), Assert.Single(value, response => response.StatusCode == 400).Type);
        Assert.Equal(typeof(ProblemDetails), Assert.Single(value, response => response.StatusCode == 422).Type);
        Assert.Equal(typeof(EarlierError), Assert.Single(value, response => response.StatusCode == 404).Type);
        var route = Route(app, "/payload-response-sets/mixed");
        Assert.Equal(RuntimeResponses(Route(native, "/payload-response-sets/mixed")), RuntimeResponses(route));
        Assert.Equal(new[] { typeof(ProblemDetails), typeof(ConfiguredError), typeof(MethodError) }, route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Where(response => response.StatusCode == 409).Select(response => response.Type));
        Assert.Equal(typeof(FilterError), Assert.Single(route.Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>(), response => response.StatusCode == 409).Type);
        var final = Assert.Single(route.Metadata.GetOrderedMetadata<FormatResponseAttribute>(), response => response.StatusCode == 409);
        Assert.Equal("final", final.Format);
        Assert.Equal(new[] { "text/plain", "application/json" }, final.ContentTypes);
    }

    [Fact]
    public async Task Fresh_mounts_keep_payload_children_independent_and_manual_aliases_do_not_import_group_contracts()
    {
        await using var app = CreatePayloads(map: false);
        foreach (var prefix in new[] { "/first", "/second" })
        {
            var group = app.MapGroup(prefix).MapMediumApiNamespace<PayloadResponseSetsGroupDefinition>();
            PayloadResponseSetScenario.MapEntries(group, suppressNames: true);
        }

        app.MapMediumApiEndpoint<PayloadResponseEndpoint>("/standalone/{value:int}").WithoutDefaultEndpointName();
        await app.StartAsync();
        var first = Route(app, "/first/payload-response-sets/value/{value:int}");
        var second = Route(app, "/second/payload-response-sets/value/{value:int}");
        foreach (var status in new[] { 409, 410, 422, 429, 431, 452, 453, 502, 503 })
        {
            Assert.NotSame(Assert.Single(first.Metadata.GetOrderedMetadata<ProblemResponseAttribute>(), response => response.StatusCode == status), Assert.Single(second.Metadata.GetOrderedMetadata<ProblemResponseAttribute>(), response => response.StatusCode == status));
        }

        Assert.NotSame(first.Metadata.GetOrderedMetadata<FormatResponseAttribute>()[0], second.Metadata.GetOrderedMetadata<FormatResponseAttribute>()[0]);
        var alias = Route(app, "/standalone/{value:int}");
        Assert.Null(alias.Metadata.GetMetadata<IEndpointNameMetadata>());
        Assert.DoesNotContain(alias.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode is 501 or 502 or 503 or 404);
        using var client = app.GetTestClient();
        Assert.Equal(new SharedReply("payload-response-set", 7), await client.GetFromJsonAsync<SharedReply>("/standalone/7"));
    }

    [Fact]
    public async Task Public_flag_selects_four_routes_while_the_original_default_and_disabled_selection_are_preserved()
    {
        await using var selected = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration["Sample:PayloadResponseSets"] = "true";
        });
        await using var original = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await using var disabled = CreatePayloads(enabled: false);
        await selected.StartAsync();
        await original.StartAsync();
        await disabled.StartAsync();
        Assert.Equal(4, Routes(selected).Count());
        Assert.Equal(8, Routes(original).Count());
        Assert.Empty(Routes(disabled));
        Assert.DoesNotContain(Routes(original), route => route.RoutePattern.RawText!.Contains("payload-response-sets", StringComparison.Ordinal));
        using var client = selected.GetTestClient();
        Assert.Equal(new SharedReply("payload-response-set", 7), await client.GetFromJsonAsync<SharedReply>("/payload-response-sets/value/7"));
    }

    private static WebApplication CreatePayloads(string reference = "generated", bool map = true, bool enabled = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        }, mapEndpoints: false);
        app.Configuration["Sample:PayloadResponseSets"] = enabled.ToString();
        if (map)
        {
            if (reference == "generated") { PayloadResponseSetScenario.Map(app); }
            else if (enabled) { MapNative(app, reference == "before"); }
        }

        return app;
    }

    private static string[] RuntimeResponses(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(response => response.StatusCode + "|" + response.Type?.AssemblyQualifiedName + "|" + string.Join(",", response.ContentTypes)).ToArray();
    }

    private static string[] Phases(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.Where(item => item is IProducesResponseTypeMetadata or ResponseSetPhaseAttribute or ResponseSetConfiguration).Select(item => item switch { ResponseSetPhaseAttribute phase => phase.Phase, ResponseSetConfiguration phase => phase.Phase, IProducesResponseTypeMetadata response => response.StatusCode.ToString(), _ => throw new InvalidOperationException("Unexpected payload phase.") }).ToArray();
    }

    private static void MapNative(WebApplication app, bool before)
    {
        var group = app.MapGroup("/payload-response-sets").WithTags(SharedResponseMetadata.Tag).WithMetadata(Response(501), Problem(502), Problem(503));
        ((IEndpointConventionBuilder)group).Finally(builder =>
        {
            foreach (var status in new[] { 400, 404 })
            {
                if (!builder.Metadata.OfType<IProducesResponseTypeMetadata>().Any(response => response.StatusCode == status)) { builder.Metadata.Add(Response(status, typeof(EarlierError), "application/json")); }
            }
        });
        AddNativePolicies(MapOriginal(group.MapGroup("").WithMetadata(Response(408), Problem(409), Problem(410)), "/value/{value:int}", before ? (Delegate)BeforeValue : NativeValue)).WithName("PayloadResponse");
        var mixed = MapOriginal(group, "/mixed", before ? (Delegate)BeforeMixed : NativeMixed).WithName("MixedPayloadResponse").WithMetadata(new EndpointResponseAttribute(409, typeof(ConfiguredError)));
        mixed.Finally(builder => builder.Metadata.Add(new FormatResponseAttribute("final", 409, typeof(MethodError), "text/plain", "application/json")));
        MapOriginal(group, "/empty", NativeEmpty).WithName("EmptyPayloadResponse");
        AddNativePolicies(MapOriginal(app.MapGroup("").WithTags(SharedResponseMetadata.Tag).WithMetadata(Response(408), Problem(409), Problem(410)), "/aliases/payload-response-sets/{value:int}", before ? (Delegate)BeforeValue : NativeValue));
    }

    private static RouteHandlerBuilder AddNativePolicies(RouteHandlerBuilder endpoint)
    {
        endpoint.Add(builder =>
        {
            if (!builder.Metadata.OfType<IProducesResponseTypeMetadata>().Any(response => response.StatusCode == 422)) { builder.Metadata.Add(Response(422, typeof(MethodError), "application/json")); }
        });
        endpoint.WithMetadata(new ResponseSetConfiguration("payload-filter-configure"), new ResponseSetPhaseAttribute("payload-filter-custom"), Response(425), Problem(429), Problem(431), Response(432));
        endpoint.AddEndpointFilter((context, next) =>
        {
            context.HttpContext.RequestServices.GetRequiredService<SharedResponseProbe>().VisitGuard();
            return context.GetArgument<int>(0) < 0 ? ValueTask.FromResult<object?>(TypedResults.Problem(statusCode: 400, title: "Use a nonnegative value.", type: "urn:sample:payload-negative")) : next(context);
        });
        endpoint.WithMetadata(new ResponseSetConfiguration("payload-factory-configure"), new ResponseSetPhaseAttribute("payload-factory-custom"), Response(451), Problem(452), Problem(453));
        return endpoint.AddEndpointFilterFactory((_, next) => next);
    }

    private static ProducesResponseTypeMetadata Problem(int status)
    {
        return Response(status, typeof(ProblemDetails), SharedResponseMetadata.ProblemJson);
    }

    private static ProducesResponseTypeMetadata Response(int status, Type? type = null, params string[] media)
    {
        return new ProducesResponseTypeMetadata(status, type ?? typeof(void), media);
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapMethods(pattern, new[] { HttpMethods.Get }, handler);
    }

    [ResponseSetPhase("payload-before")]
    [ProblemResponse(400)]
    [ProblemResponse(422)]
    [FormatResponse("text", 418, typeof(string), "text/plain", "text/csv")]
    [FormatResponse("structured", 419, typeof(FilterError), "application/xml", "application/json")]
    [FormatResponse("text", 418, typeof(string), "text/plain", "text/csv")]
    [ResponseSetPhase("payload-after")]
    private static Ok<SharedReply> BeforeValue([FromRoute] int value, SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "payload-response-set", value);
    }

    [ResponseSetPhase("payload-before")]
    [NativePayloadResponse(400, typeof(ProblemDetails), SharedResponseMetadata.ProblemJson)]
    [NativePayloadResponse(422, typeof(ProblemDetails), SharedResponseMetadata.ProblemJson)]
    [NativePayloadResponse(418, typeof(string), "text/plain", "text/csv")]
    [NativePayloadResponse(419, typeof(FilterError), "application/xml", "application/json")]
    [NativePayloadResponse(418, typeof(string), "text/plain", "text/csv")]
    [ResponseSetPhase("payload-after")]
    private static Ok<SharedReply> NativeValue([FromRoute] int value, SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "payload-response-set", value);
    }

    [ProblemResponse(400)]
    [ProblemResponse(409)]
    [ProducesResponseType<FilterError>(409, "application/json")]
    private static Results<Ok<SharedReply>, BadRequest<FilterError>> BeforeMixed(SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "payload-response-set-mixed", 0);
    }

    [NativePayloadResponse(400, typeof(ProblemDetails), SharedResponseMetadata.ProblemJson)]
    [NativePayloadResponse(409, typeof(ProblemDetails), SharedResponseMetadata.ProblemJson)]
    [ProducesResponseType<FilterError>(409, "application/json")]
    private static Results<Ok<SharedReply>, BadRequest<FilterError>> NativeMixed(SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "payload-response-set-mixed", 0);
    }

    private static Ok<SharedReply> NativeEmpty(SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "payload-response-set-empty", 0);
    }

    private static Ok<SharedReply> PhaseValue(int value)
    {
        return TypedResults.Ok(new SharedReply("payload-phase", value));
    }

    private sealed class SuppliedResponsesAttribute : EndpointResponsesAttribute
    {
        public SuppliedResponsesAttribute(IReadOnlyList<EndpointResponseAttribute> responses) : base(responses) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    private sealed class NativePayloadResponseAttribute : Attribute, IProducesResponseTypeMetadata
    {
        private readonly ProducesResponseTypeMetadata metadata;

        public NativePayloadResponseAttribute(int status, Type type, params string[] media)
        {
            metadata = Response(status, type, media);
        }

        public int StatusCode => metadata.StatusCode;
        public Type? Type => metadata.Type;
        public IEnumerable<string> ContentTypes => metadata.ContentTypes;
    }
}