using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
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
using RonSijm.MediumAPI.SharedResponsesExample.DerivedResponses;
using RonSijm.MediumAPI.SharedResponsesExample.DerivedResponses.Disabled;
using Xunit;
using static RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.SharedResponsesHost;
using ExampleProgram = RonSijm.MediumAPI.SharedResponsesExample.Program;

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest;

public sealed class DerivedResponsePolicyTests
{
    [Theory]
    [InlineData(7, 200)]
    [InlineData(-1, 400)]
    public async Task Derived_problem_declarations_preserve_actual_typed_success_and_native_filter_outcomes(int value, int status)
    {
        await using var native = CreateDerived(native: true);
        await using var app = CreateDerived();
        await native.StartAsync();
        await app.StartAsync();
        using var expectedClient = native.GetTestClient();
        using var client = app.GetTestClient();
        using var expected = await expectedClient.GetAsync("/derived-responses/value/" + value);
        using var actual = await client.GetAsync("/derived-responses/value/" + value);
        Assert.Equal(status, (int)actual.StatusCode);
        Assert.Equal(expected.Content.Headers.ContentType?.ToString(), actual.Content.Headers.ContentType?.ToString());
        Assert.Equal(await expected.Content.ReadAsByteArrayAsync(), await actual.Content.ReadAsByteArrayAsync());
        Assert.Equal(status == 200 ? 1 : 0, Probe(app).TotalCalls);
        Assert.Equal(1, Probe(app).GuardVisits);
    }

    [Theory]
    [InlineData("/derived-responses/inherited/7", "derived-inherited", 7)]
    [InlineData("/derived-responses/mixed", "derived-mixed", 0)]
    [InlineData("/derived-responses/formats", "derived-formats", 0)]
    [InlineData("/aliases/derived/7", "derived", 7)]
    public async Task Inherited_method_formats_MVC_and_alias_metadata_do_not_change_HTTP_payloads(string path, string source, int value)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = CreateDerived(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new SharedReply(source, value), await client.GetFromJsonAsync<SharedReply>(path));
        }
    }

    [Fact]
    public async Task Ordered_native_response_contracts_API_Explorer_and_complete_provider_documents_match()
    {
        await using var native = CreateDerived(native: true);
        await using var app = CreateDerived();
        await native.StartAsync();
        await app.StartAsync();
        Assert.Equal(5, Routes(app).Count());
        Assert.Equal(MetadataInventory(native), MetadataInventory(app));
        foreach (var expected in Routes(native)) { Assert.Equal(RuntimeResponses(expected), RuntimeResponses(Route(app, expected.RoutePattern.RawText!))); }

        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(app)));
        Equivalent(MediumApiContractComparison.CompareOpenApi(OpenApiJson(native), OpenApiJson(app)));
        Assert.Equal(0, Probe(app).TotalCalls);
    }

    [Fact]
    public async Task Dual_marker_class_method_and_namespace_declarations_attach_once_at_their_existing_response_positions()
    {
        await using var app = CreateDerived();
        await app.StartAsync();
        var metadata = Route(app, "/derived-responses/value/{value:int}").Metadata;
        Assert.Equal(1, metadata.GetOrderedMetadata<MarkedProblemResponseAttribute>().Count(value => value.StatusCode == 502));
        Assert.Equal(2, metadata.GetOrderedMetadata<MarkedProblemResponseAttribute>().Count(value => value.StatusCode == 409));
        Assert.Equal(1, metadata.GetOrderedMetadata<MarkedProblemResponseAttribute>().Count(value => value.StatusCode == 403));
        Assert.Equal(1, metadata.GetOrderedMetadata<MarkedProblemResponseAttribute>().Count(value => value.StatusCode == 410));
        Assert.Equal(1, metadata.GetOrderedMetadata<MarkedProblemResponseAttribute>().Count(value => value.StatusCode == 422));
        var inherited = Route(app, "/derived-responses/inherited/{value:int}").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        Assert.DoesNotContain(inherited, value => value.StatusCode == 451);
        Assert.Single(inherited, value => value.StatusCode == 412);
        Assert.Single(inherited, value => value.StatusCode == 415);
    }

    [Theory]
    [InlineData("endpoint")]
    [InlineData("group")]
    public async Task Mixed_dual_marker_filter_and_factory_responses_construct_once_and_keep_the_response_phase(string scope)
    {
        await using var app = CreateDerived(map: false);
        IEndpointConventionBuilder builder;
        RouteGroupBuilder? group = null;
        if (scope == "group")
        {
            group = app.MapGroup("");
            builder = group;
        }
        else { builder = app.MapGet("/phase", NativeNoContent); }

        var statuses = new[] { 409, 429, 403, 410 };
        var before = statuses.ToDictionary(status => status, MarkedProblemResponseAttribute.Constructions);
        builder.AddMediumApiFilter<DerivedProblemFilter>().AddMediumApiFilterFactory<DerivedProblemFactory>();
        foreach (var status in statuses) { Assert.Equal(before[status] + 1, MarkedProblemResponseAttribute.Constructions(status)); }

        if (group is not null) { group.MapGet("/phase", NativeNoContent); }

        await app.StartAsync();
        var phases = Route(app, "/phase").Metadata.Where(value => value is ResponseConfigurationPhase or ResponsePhaseAttribute || value is IProducesResponseTypeMetadata response && response.StatusCode != 204).Select(Phase).ToArray();
        Assert.Equal(new[] { "configure-filter", "custom-filter", "400", "409", "422", "429", "configure-factory", "custom-factory", "401", "403", "406", "410" }, phases);
        foreach (var status in statuses) { Assert.Equal(before[status] + 1, MarkedProblemResponseAttribute.Constructions(status)); }
    }

    [Fact]
    public void Derived_constructors_retain_status_JSON_defaults_multiple_media_and_read_only_copies()
    {
        Assert.Equal(typeof(void), new DerivedResponseAttribute(204).Type);
        Assert.Empty(new DerivedResponseAttribute(204).ContentTypes);
        Assert.Equal(new[] { "application/json" }, new DerivedResponseAttribute(200, typeof(SharedReply), "").ContentTypes);
        var additional = new[] { "application/json" };
        var response = new DerivedResponseAttribute(201, typeof(string), "text/plain", additional);
        additional[0] = "application/xml";
        Assert.Equal(new[] { "text/plain", "application/json" }, response.ContentTypes);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)response.ContentTypes)[0] = "application/xml");
        Assert.Null(typeof(EndpointResponseAttribute).GetProperty(nameof(EndpointResponseAttribute.StatusCode))!.SetMethod);
        Assert.Null(typeof(EndpointResponseAttribute).GetProperty(nameof(EndpointResponseAttribute.Type))!.SetMethod);
        Assert.Equal(typeof(ProblemDetails), new ProblemResponseAttribute(409).Type);
        Assert.Equal(new[] { SharedResponseMetadata.ProblemJson }, new ProblemResponseAttribute(409).ContentTypes);
    }

    [Theory]
    [InlineData("*/*")]
    [InlineData("text/*")]
    [InlineData("not a media type")]
    public void Derived_aliases_keep_native_media_validation_exception_types(string media)
    {
        var expected = Record.Exception(() => new EndpointResponseAttribute(400, typeof(string), media));
        var actual = Record.Exception(() => new DerivedResponseAttribute(400, typeof(string), media));
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.Equal(expected.GetType(), actual.GetType());
    }

    [Fact]
    public void Null_additional_media_retains_the_original_constructor_boundary()
    {
        Assert.Throws<ArgumentNullException>(() => new DerivedResponseAttribute(400, typeof(string), "text/plain", null!));
        Assert.Empty(new EndpointResponseAttribute(204, additionalContentTypes: null!).ContentTypes);
        Assert.Empty(new DerivedResponseAttribute(204, additionalContentTypes: null!).ContentTypes);
    }

    [Fact]
    public async Task Repeated_mounts_create_fresh_attributes_and_aliases_do_not_import_namespace_responses()
    {
        await using var app = CreateDerived(map: false);
        foreach (var prefix in new[] { "/first", "/second" })
        {
            var group = app.MapGroup(prefix).MapMediumApiNamespace<DerivedResponseGroupDefinition>();
            DerivedResponseScenario.MapEntries(group, suppressNames: true);
        }

        app.MapMediumApiEndpoint<DerivedProblemEndpoint>("/standalone/{value:int}").WithoutDefaultEndpointName();
        await app.StartAsync();
        var first = Route(app, "/first/derived-responses/value/{value:int}").Metadata.GetOrderedMetadata<MarkedProblemResponseAttribute>();
        var second = Route(app, "/second/derived-responses/value/{value:int}").Metadata.GetOrderedMetadata<MarkedProblemResponseAttribute>();
        Assert.Equal(first.Count, second.Count);
        for (var index = 0; index < first.Count; index++) { Assert.NotSame(first[index], second[index]); }

        var standalone = Route(app, "/standalone/{value:int}");
        Assert.DoesNotContain(standalone.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), value => value.StatusCode is 502 or 503);
        Assert.Null(standalone.Metadata.GetMetadata<IEndpointNameMetadata>());
    }

    [Fact]
    public async Task Disabled_derived_declarations_are_not_constructed_and_default_routes_remain_unchanged()
    {
        await using var app = CreateDerived(enabled: false);
        Assert.Null(app.TryMapMediumApiNamespace<DisabledDerivedResponseGroupDefinition>());
        await app.StartAsync();
        Assert.Empty(Routes(app));
        await using var original = SharedResponsesHost.Create();
        await original.StartAsync();
        Assert.Equal(8, Routes(original).Count());
    }

    [Fact]
    public async Task Derived_statuses_satisfy_fallbacks_and_native_MVC_response_providers_keep_their_separate_contract()
    {
        await using var app = CreateDerived();
        await app.StartAsync();
        var value = Route(app, "/derived-responses/value/{value:int}").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        Assert.Equal(typeof(ProblemDetails), Assert.Single(value, response => response.StatusCode == 404).Type);
        var mixed = Route(app, "/derived-responses/mixed").Metadata;
        Assert.Equal(typeof(EarlierError), Assert.Single(mixed.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 404).Type);
        Assert.Equal(typeof(ProblemDetails), Assert.Single(mixed.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 409).Type);
        Assert.Equal(typeof(FilterError), Assert.Single(mixed.GetOrderedMetadata<IApiResponseMetadataProvider>(), response => response.StatusCode == 409).Type);
    }

    private static WebApplication CreateDerived(bool native = false, bool enabled = true, bool map = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        }, mapEndpoints: false);
        app.Configuration["Sample:DerivedResponses"] = enabled.ToString();
        if (map)
        {
            if (native) { MapNative(app, enabled); }
            else { DerivedResponseScenario.Map(app); }
        }

        return app;
    }

    private static string[] RuntimeResponses(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(value => value.StatusCode + "|" + value.Type?.AssemblyQualifiedName + "|" + string.Join(",", value.ContentTypes)).ToArray();
    }

    private static string Phase(object metadata)
    {
        return metadata switch { ResponseConfigurationPhase phase => phase.Phase, ResponsePhaseAttribute phase => phase.Phase, IProducesResponseTypeMetadata response => response.StatusCode.ToString(), _ => throw new InvalidOperationException("Unexpected response phase.") };
    }

    private static void MapNative(WebApplication app, bool enabled)
    {
        if (!enabled) { return; }

        var group = app.MapGroup("/derived-responses").WithTags(SharedResponseMetadata.Tag);
        group.WithMetadata(Response(503), Response(502));
        ((IEndpointConventionBuilder)group).Finally(builder =>
        {
            if (!builder.Metadata.OfType<IProducesResponseTypeMetadata>().Any(response => response.StatusCode == 404)) { builder.Metadata.Add(new ProducesResponseTypeMetadata(404, typeof(EarlierError), new[] { "application/json" })); }
        });
        AddNativePolicies(MapOriginal(group.MapGroup("").WithMetadata(Response(400), Response(409)), "/value/{value:int}", NativeValue)).WithName("DerivedProblem");
        MapOriginal(group, "/inherited/{value:int}", NativeInherited).WithName("InheritedDerivedResponse");
        MapOriginal(group.MapGroup("").WithMetadata(Response(409)), "/mixed", NativeMixed).WithName("MixedDerivedResponse");
        MapOriginal(group.MapGroup("").WithMetadata(new ProducesResponseTypeMetadata(418, typeof(void), Array.Empty<string>()), new ProducesResponseTypeMetadata(419, typeof(FilterError), new[] { "application/json" }), new ProducesResponseTypeMetadata(420, typeof(string), new[] { "text/plain", "application/json" })), "/formats", NativeFormats).WithName("DerivedFormats");
        AddNativePolicies(MapOriginal(app.MapGroup("").WithTags(SharedResponseMetadata.Tag).WithMetadata(Response(400), Response(409)), "/aliases/derived/{value:int}", NativeValue));
    }

    private static RouteHandlerBuilder AddNativePolicies(RouteHandlerBuilder endpoint)
    {
        endpoint.WithMetadata(new ResponseConfigurationPhase("configure-filter"), new ResponsePhaseAttribute("custom-filter"), Response(400), Response(409), Response(422), Response(429));
        endpoint.AddEndpointFilter((context, next) =>
        {
            context.HttpContext.RequestServices.GetRequiredService<SharedResponseProbe>().VisitGuard();
            return context.GetArgument<int>(0) < 0 ? ValueTask.FromResult<object?>(TypedResults.Problem(statusCode: 400, title: "Use a nonnegative value.", type: "urn:sample:derived-negative")) : next(context);
        });
        endpoint.WithMetadata(new ResponseConfigurationPhase("configure-factory"), new ResponsePhaseAttribute("custom-factory"), Response(401), Response(403), Response(406), Response(410));
        return endpoint.AddEndpointFilterFactory((_, next) => next);
    }

    private static ProducesResponseTypeMetadata Response(int status)
    {
        return new ProducesResponseTypeMetadata(status, typeof(ProblemDetails), new[] { SharedResponseMetadata.ProblemJson });
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapMethods(pattern, new[] { "GET" }, handler);
    }

    [NativeProblemDeclaration(404)]
    [NativeProblemDeclaration(422)]
    private static Ok<SharedReply> NativeValue(int value, SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "derived", value);
    }

    [NativeProblemDeclaration(412)]
    [NativeProblemDeclaration(415)]
    private static Ok<SharedReply> NativeInherited(int value, SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "derived-inherited", value);
    }

    [ProducesResponseType<FilterError>(409, "application/json")]
    private static Ok<SharedReply> NativeMixed(SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "derived-mixed", 0);
    }

    private static Ok<SharedReply> NativeFormats(SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "derived-formats", 0);
    }

    private static NoContent NativeNoContent()
    {
        return TypedResults.NoContent();
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class NativeProblemDeclarationAttribute : Attribute, IProducesResponseTypeMetadata
    {
        private readonly ProducesResponseTypeMetadata metadata;

        public NativeProblemDeclarationAttribute(int status)
        {
            metadata = Response(status);
        }

        public int StatusCode => metadata.StatusCode;
        public Type? Type => metadata.Type;
        public IEnumerable<string> ContentTypes => metadata.ContentTypes;
    }
}