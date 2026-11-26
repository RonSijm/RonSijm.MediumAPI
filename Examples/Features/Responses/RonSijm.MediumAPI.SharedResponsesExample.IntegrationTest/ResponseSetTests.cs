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
using RonSijm.MediumAPI.SharedResponsesExample.ResponseSets;
using RonSijm.MediumAPI.SharedResponsesExample.ResponseSets.Disabled;
using Xunit;
using static RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.SharedResponsesHost;
using ExampleProgram = RonSijm.MediumAPI.SharedResponsesExample.Program;

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest;

public sealed class ResponseSetTests
{
    [Fact]
    public void Constructor_copies_statuses_and_retains_order_duplicates_ordinary_children_and_read_only_values()
    {
        var statuses = new[] { 401, 403, 401 };
        var set = new EndpointResponsesAttribute(statuses);
        statuses[0] = 500;
        Assert.Equal(new[] { 401, 403, 401 }, set.Responses.Select(response => response.StatusCode));
        Assert.All(set.Responses, response =>
        {
            Assert.IsType<EndpointResponseAttribute>(response);
            Assert.Equal(typeof(void), response.Type);
            Assert.Empty(response.ContentTypes);
        });
        Assert.NotSame(set.Responses[0], set.Responses[2]);
        Assert.NotSame(set.Responses[0], new AccessResponsesAttribute().Responses[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<EndpointResponseAttribute>)set.Responses)[0] = new EndpointResponseAttribute(500));
        Assert.Null(typeof(EndpointResponsesAttribute).GetProperty(nameof(EndpointResponsesAttribute.Responses))!.SetMethod);
    }

    [Fact]
    public void Empty_sets_are_valid_null_arrays_fail_and_native_attribute_owner_contract_is_explicit()
    {
        Assert.Empty(new EndpointResponsesAttribute().Responses);
        Assert.Equal("statusCodes", Assert.Throws<ArgumentNullException>(() => new EndpointResponsesAttribute(null!)).ParamName);
        var usage = typeof(EndpointResponsesAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        Assert.Equal(AttributeTargets.Class | AttributeTargets.Method, usage.ValidOn);
        Assert.True(usage.AllowMultiple);
        Assert.True(usage.Inherited);
        Assert.False(typeof(EndpointResponsesAttribute).IsSealed);
        Assert.False(typeof(IProducesResponseTypeMetadata).IsAssignableFrom(typeof(EndpointResponsesAttribute)));
    }

    [Theory]
    [InlineData(7, 200)]
    [InlineData(-1, 400)]
    public async Task Reusable_response_sets_retain_HTTP_success_filter_errors_media_and_payload_bytes(int value, int status)
    {
        byte[]? expectedBody = null;
        string? expectedMedia = null;
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreateSets(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/response-sets/value/" + value);
            Assert.Equal(status, (int)response.StatusCode);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var media = response.Content.Headers.ContentType?.ToString();
            if (expectedBody is null)
            {
                expectedBody = bytes;
                expectedMedia = media;
            }
            else
            {
                Assert.Equal(expectedBody, bytes);
                Assert.Equal(expectedMedia, media);
            }

            Assert.Equal(status == 200 ? 1 : 0, Probe(app).TotalCalls);
            Assert.Equal(1, Probe(app).GuardVisits);
        }
    }

    [Theory]
    [InlineData("/response-sets/inherited/7", "response-set-inherited", 7)]
    [InlineData("/response-sets/typed", "response-set-typed", 0)]
    [InlineData("/response-sets/empty", "response-set-empty", 0)]
    [InlineData("/aliases/response-sets/7", "response-set", 7)]
    public async Task Inherited_typed_empty_and_alias_handlers_keep_their_actual_HTTP_contracts(string path, string source, int value)
    {
        foreach (var reference in new[] { "before", "native", "generated" })
        {
            await using var app = CreateSets(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new SharedReply(source, value), await client.GetFromJsonAsync<SharedReply>(path));
        }
    }

    [Fact]
    public async Task Ordered_raw_contracts_API_Explorer_and_complete_OpenAPI_documents_match_both_independent_references()
    {
        await using var app = CreateSets();
        await app.StartAsync();
        foreach (var reference in new[] { "before", "native" })
        {
            await using var expected = CreateSets(reference);
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

        Assert.Equal(5, Routes(app).Count());
        Assert.Equal(0, Probe(app).TotalCalls);
    }

    [Fact]
    public async Task Method_sets_expand_in_place_before_later_callbacks_without_leaking_marker_interfaces_or_losing_duplicates()
    {
        await using var app = CreateSets();
        await app.StartAsync();
        var endpoint = Route(app, "/response-sets/value/{value:int}");
        var phases = Phases(endpoint);
        var start = Array.IndexOf(phases, "method-before");
        Assert.True(start >= 0);
        Assert.Equal(new[] { "method-before", "418", "401", "403", "401", "method-after", "422" }, phases.Skip(start).Take(7));
        Assert.All(Routes(app), route => Assert.Empty(route.Metadata.OfType<EndpointResponsesAttribute>()));
        Assert.All(Routes(app), route => Assert.Equal(new[] { SharedResponseMetadata.Tag }, route.Metadata.GetMetadata<ITagsMetadata>()!.Tags));
        Assert.False(Assert.Single(endpoint.Metadata.OfType<ResponseSetObservation>()).ContainsAuthoringMarker);
        var method = endpoint.Metadata.GetMetadata<MethodInfo>()!;
        Assert.Equal(typeof(ResponseSetValueEndpoint), method.DeclaringType);
        Assert.Equal("ResponseSetValue", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(2, method.GetCustomAttributes<EndpointResponsesAttribute>().Count());
        Assert.Equal(new[] { 418, 422 }, method.GetCustomAttributes<EndpointResponseAttribute>().Select(response => response.StatusCode));
        foreach (var response in endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Where(response => response.StatusCode is 401 or 403 or 410 or 411 or 429 or 431 or 451 or 452 or 453))
        {
            Assert.IsType<EndpointResponseAttribute>(response);
            Assert.Equal(typeof(void), response.Type);
            Assert.Empty(response.ContentTypes);
        }
    }

    [Theory]
    [InlineData("endpoint")]
    [InlineData("group")]
    public async Task Combined_filter_and_factory_registration_preserve_custom_then_mixed_response_phases_through_interface_builders(string scope)
    {
        await using var app = CreateSets(map: false);
        IEndpointConventionBuilder builder;
        RouteGroupBuilder? group = null;
        if (scope == "group")
        {
            group = app.MapGroup("");
            builder = group;
        }
        else { builder = app.MapGet("/phase/{value:int}", PhaseValue); }

        builder.AddMediumApiFilter<ResponseSetFilter>().AddMediumApiFilterFactory<ResponseSetFactory>();
        if (group is not null) { group.MapGet("/phase/{value:int}", PhaseValue); }

        await app.StartAsync();
        var route = Route(app, "/phase/{value:int}");
        Assert.Equal(new[] { "filter-configure", "filter-custom", "425", "429", "431", "432", "400", "factory-configure", "observed:false", "factory-custom", "451", "452", "453" }, Phases(route).Where(phase => phase != "200"));
        Assert.Empty(route.Metadata.OfType<EndpointResponsesAttribute>());
        Assert.Null(route.Metadata.GetMetadata<ITagsMetadata>());
        using var client = app.GetTestClient();
        Assert.Equal(new SharedReply("phase", 7), await client.GetFromJsonAsync<SharedReply>("/phase/7"));
    }

    [Fact]
    public async Task Direct_class_owners_ignore_base_sets_while_original_inherited_method_sets_remain()
    {
        await using var app = CreateSets();
        await app.StartAsync();
        var route = Route(app, "/response-sets/inherited/{value:int}");
        Assert.Equal(typeof(ResponseSetEndpointBase), route.Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        Assert.Equal(typeof(InheritedResponseSetEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        var responses = route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        Assert.Single(responses, response => response.StatusCode == 406);
        Assert.Single(responses, response => response.StatusCode == 407);
        Assert.DoesNotContain(responses, response => response.StatusCode is 497 or 499);
        await using var filterHost = CreateSets(map: false);
        filterHost.MapGet("/filter", PhaseNoContent).AddMediumApiFilter<InheritedResponseSetFilter>();
        await filterHost.StartAsync();
        Assert.DoesNotContain(Route(filterHost, "/filter").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 498);
    }

    [Fact]
    public async Task Fresh_mounts_keep_independent_response_children_and_aliases_do_not_import_namespace_sets()
    {
        await using var app = CreateSets(map: false);
        foreach (var prefix in new[] { "/first", "/second" })
        {
            var group = app.MapGroup(prefix).MapMediumApiNamespace<ResponseSetsGroupDefinition>();
            ResponseSetScenario.MapEntries(group, suppressNames: true);
        }

        app.MapMediumApiEndpoint<ResponseSetValueEndpoint>("/standalone/{value:int}").WithoutDefaultEndpointName();
        await app.StartAsync();
        var first = Route(app, "/first/response-sets/value/{value:int}").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Where(response => response.StatusCode is 500 or 503 or 410 or 411 or 401 or 403).ToArray();
        var second = Route(app, "/second/response-sets/value/{value:int}").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Where(response => response.StatusCode is 500 or 503 or 410 or 411 or 401 or 403).ToArray();
        Assert.Equal(first.Length, second.Length);
        for (var index = 0; index < first.Length; index++) { Assert.NotSame(first[index], second[index]); }

        var standalone = Route(app, "/standalone/{value:int}");
        Assert.DoesNotContain(standalone.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode is 500 or 503 or 404);
        Assert.Null(standalone.Metadata.GetMetadata<IEndpointNameMetadata>());
        using var client = app.GetTestClient();
        Assert.Equal(new SharedReply("response-set", 7), await client.GetFromJsonAsync<SharedReply>("/standalone/7"));
    }

    [Fact]
    public async Task Disabled_owners_do_not_construct_sets_and_a_later_enabled_mapping_is_independent()
    {
        await using var app = CreateSets(enabled: false);
        Assert.Null(app.TryMapMediumApiNamespace<DisabledResponseSetsGroupDefinition>());
        Assert.Empty(Routes(app));
        app.Configuration["Sample:ResponseSets"] = "true";
        ResponseSetScenario.Map(app);
        await app.StartAsync();
        Assert.Equal(5, Routes(app).Count());
    }

    [Fact]
    public async Task Typed_native_MVC_fluent_final_and_response_fallback_contracts_keep_their_existing_priorities()
    {
        await using var app = CreateSets();
        await using var native = CreateSets("native");
        await app.StartAsync();
        await native.StartAsync();
        var value = Route(app, "/response-sets/value/{value:int}").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        Assert.Equal(2, value.Count(response => response.StatusCode == 401));
        Assert.Equal(typeof(void), Assert.Single(value, response => response.StatusCode == 403).Type);
        Assert.Equal(typeof(EarlierError), Assert.Single(value, response => response.StatusCode == 404).Type);
        var typed = Route(app, "/response-sets/typed").Metadata;
        var nativeTyped = Route(native, "/response-sets/typed").Metadata;
        Assert.Equal(nativeTyped.GetOrderedMetadata<IProducesResponseTypeMetadata>().Count(response => response.StatusCode == 401), typed.GetOrderedMetadata<IProducesResponseTypeMetadata>().Count(response => response.StatusCode == 401));
        Assert.Equal(RuntimeResponses(Route(native, "/response-sets/typed")), RuntimeResponses(Route(app, "/response-sets/typed")));
        Assert.Equal(new[] { typeof(void), typeof(ConfiguredError), typeof(MethodError) }, typed.GetOrderedMetadata<IProducesResponseTypeMetadata>().Where(response => response.StatusCode == 403).Select(response => response.Type));
        Assert.Equal(typeof(FilterError), Assert.Single(typed.GetOrderedMetadata<IApiResponseMetadataProvider>(), response => response.StatusCode == 403).Type);
    }

    [Fact]
    public async Task Bare_native_methods_and_WithMetadata_do_not_implicitly_expand_authoring_sets()
    {
        await using var app = CreateSets(map: false);
        var supplied = new EndpointResponsesAttribute(418, 419);
        MapOriginal(app, "/native-method", BareNativeValue);
        app.MapGet("/native-metadata", PhaseNoContent).WithMetadata(supplied);
        var earlier = new EndpointResponsesAttribute(450);
        var later = new EndpointResponsesAttribute(460);
        app.MapGroup("/injected").WithMetadata(earlier).MapMediumApiEndpoint<ResponseSetValueEndpoint>().WithoutDefaultEndpointName().WithMetadata(later);
        await app.StartAsync();
        Assert.Single(Route(app, "/native-method").Metadata.OfType<EndpointResponsesAttribute>());
        Assert.Same(supplied, Assert.Single(Route(app, "/native-metadata").Metadata.OfType<EndpointResponsesAttribute>()));
        Assert.DoesNotContain(Route(app, "/native-method").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode is 401 or 403);
        Assert.DoesNotContain(Route(app, "/native-metadata").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode is 418 or 419);
        var mapped = Route(app, "/injected/value/{value:int}").Metadata;
        Assert.Same(earlier.Responses[0], Assert.Single(mapped.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 450));
        Assert.Same(later, Assert.Single(mapped.OfType<EndpointResponsesAttribute>()));
        Assert.DoesNotContain(mapped.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 460);
    }

    [Fact]
    public async Task Public_flag_selects_the_five_route_scenario_and_retains_the_original_eight_route_example()
    {
        await using var selected = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration["Sample:ResponseSets"] = "true";
        });
        await using var original = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await selected.StartAsync();
        await original.StartAsync();
        Assert.Equal(5, Routes(selected).Count());
        Assert.Equal(8, Routes(original).Count());
        Assert.DoesNotContain(Routes(original), route => route.RoutePattern.RawText!.Contains("response-sets", StringComparison.Ordinal));
        using var client = selected.GetTestClient();
        Assert.Equal(new SharedReply("response-set", 7), await client.GetFromJsonAsync<SharedReply>("/response-sets/value/7"));
    }

    private static WebApplication CreateSets(string reference = "generated", bool enabled = true, bool map = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        }, mapEndpoints: false);
        app.Configuration["Sample:ResponseSets"] = enabled.ToString();
        if (map)
        {
            if (reference == "generated") { ResponseSetScenario.Map(app); }
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
        return endpoint.Metadata.Where(item => item is IProducesResponseTypeMetadata or ResponseSetPhaseAttribute or ResponseSetConfiguration or ResponseSetObservation).Select(item => item switch { ResponseSetPhaseAttribute phase => phase.Phase, ResponseSetConfiguration phase => phase.Phase, ResponseSetObservation observation => "observed:" + observation.ContainsAuthoringMarker.ToString().ToLowerInvariant(), IProducesResponseTypeMetadata response => response.StatusCode.ToString(), _ => throw new InvalidOperationException("Unexpected response phase.") }).ToArray();
    }

    private static void MapNative(WebApplication app, bool before)
    {
        var group = app.MapGroup("/response-sets").WithTags(SharedResponseMetadata.Tag).WithMetadata(Response(500), Response(503));
        ((IEndpointConventionBuilder)group).Finally(builder =>
        {
            foreach (var status in new[] { 401, 404 })
            {
                if (!builder.Metadata.OfType<IProducesResponseTypeMetadata>().Any(response => response.StatusCode == status)) { builder.Metadata.Add(Response(status, typeof(EarlierError))); }
            }
        });
        AddNativePolicies(MapOriginal(group.MapGroup("").WithMetadata(Response(409), Response(410), Response(411), Response(412)), "/value/{value:int}", before ? (Delegate)BeforeValue : NativeValue)).WithName("ResponseSetValue");
        MapOriginal(group, "/inherited/{value:int}", before ? (Delegate)BeforeInherited : NativeInherited).WithName("InheritedResponseSet");
        var typed = MapOriginal(group, "/typed", before ? (Delegate)BeforeTyped : NativeTyped).WithName("TypedResponseSet").WithMetadata(new EndpointResponseAttribute(403, typeof(ConfiguredError)));
        typed.Finally(builder => builder.Metadata.Add(new EndpointResponseAttribute(403, typeof(MethodError))));
        MapOriginal(group, "/empty", NativeEmpty).WithName("EmptyResponseSet");
        AddNativePolicies(MapOriginal(app.MapGroup("").WithTags(SharedResponseMetadata.Tag).WithMetadata(Response(409), Response(410), Response(411), Response(412)), "/aliases/response-sets/{value:int}", before ? (Delegate)BeforeValue : NativeValue));
    }

    private static RouteHandlerBuilder AddNativePolicies(RouteHandlerBuilder endpoint)
    {
        // Method 403 already exists before this ordinary fallback registration.
        endpoint.Add(builder =>
        {
            if (!builder.Metadata.OfType<IProducesResponseTypeMetadata>().Any(response => response.StatusCode == 403)) { builder.Metadata.Add(Response(403, typeof(MethodError))); }
        });
        endpoint.WithMetadata(new ResponseSetConfiguration("filter-configure"), new ResponseSetPhaseAttribute("filter-custom"), Response(425), Response(429), Response(431), Response(432), Response(400, typeof(FilterError)));
        endpoint.AddEndpointFilter((context, next) =>
        {
            context.HttpContext.RequestServices.GetRequiredService<SharedResponseProbe>().VisitGuard();
            return context.GetArgument<int>(0) < 0 ? ValueTask.FromResult<object?>(TypedResults.BadRequest(new FilterError("negative", "Use a nonnegative value."))) : next(context);
        });
        endpoint.WithMetadata(new ResponseSetConfiguration("factory-configure"));
        endpoint.Add(builder => builder.Metadata.Add(new ResponseSetObservation(builder.Metadata.OfType<EndpointResponsesAttribute>().Any())));
        endpoint.WithMetadata(new ResponseSetPhaseAttribute("factory-custom"), Response(451), Response(452), Response(453));
        return endpoint.AddEndpointFilterFactory((_, next) => next);
    }

    private static ProducesResponseTypeMetadata Response(int status, Type? type = null)
    {
        return new ProducesResponseTypeMetadata(status, type ?? typeof(void), type is null ? Array.Empty<string>() : new[] { "application/json" });
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapMethods(pattern, new[] { HttpMethods.Get }, handler);
    }

    [ResponseSetPhase("method-before")]
    [EndpointResponse(418)]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(401)]
    [ResponseSetPhase("method-after")]
    [EndpointResponse(422)]
    private static Ok<SharedReply> BeforeValue([FromRoute] int value, SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "response-set", value);
    }

    [ResponseSetPhase("method-before")]
    [NativeStatusResponse(418)]
    [NativeStatusResponse(401)]
    [NativeStatusResponse(403)]
    [NativeStatusResponse(401)]
    [ResponseSetPhase("method-after")]
    [NativeStatusResponse(422)]
    private static Ok<SharedReply> NativeValue([FromRoute] int value, SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "response-set", value);
    }

    [EndpointResponse(406)]
    [EndpointResponse(407)]
    private static Ok<SharedReply> BeforeInherited([FromRoute] int value, SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "response-set-inherited", value);
    }

    [NativeStatusResponse(406)]
    [NativeStatusResponse(407)]
    private static Ok<SharedReply> NativeInherited([FromRoute] int value, SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "response-set-inherited", value);
    }

    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [ProducesResponseType<FilterError>(403, "application/json")]
    private static Results<Ok<SharedReply>, UnauthorizedHttpResult> BeforeTyped(SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "response-set-typed", 0);
    }

    [NativeStatusResponse(401)]
    [NativeStatusResponse(403)]
    [ProducesResponseType<FilterError>(403, "application/json")]
    private static Results<Ok<SharedReply>, UnauthorizedHttpResult> NativeTyped(SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "response-set-typed", 0);
    }

    private static Ok<SharedReply> NativeEmpty(SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "response-set-empty", 0);
    }

    private static Ok<SharedReply> PhaseValue(int value)
    {
        return TypedResults.Ok(new SharedReply("phase", value));
    }

    private static NoContent PhaseNoContent()
    {
        return TypedResults.NoContent();
    }

    [EndpointResponses(401, 403)]
    private static NoContent BareNativeValue()
    {
        return TypedResults.NoContent();
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    private sealed class NativeStatusResponseAttribute : Attribute, IProducesResponseTypeMetadata
    {
        private readonly ProducesResponseTypeMetadata metadata;

        public NativeStatusResponseAttribute(int status)
        {
            metadata = Response(status);
        }

        public int StatusCode => metadata.StatusCode;
        public Type? Type => metadata.Type;
        public IEnumerable<string> ContentTypes => metadata.ContentTypes;
    }
}