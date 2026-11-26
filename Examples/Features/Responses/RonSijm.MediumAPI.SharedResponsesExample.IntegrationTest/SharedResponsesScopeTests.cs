using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Composed.Child;
using RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Contracts;
using RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Disabled;
using RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Disabled.Child;
using RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Formats;
using RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Fresh;
using RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Ignored;
using RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Inheritance;
using RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Manual;
using RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.MetadataOnly;
using RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Ordered;
using RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Scoped;
using Xunit;

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest;

public sealed class SharedResponsesScopeTests
{
    [Fact]
    public async Task Ordered_declarations_retain_group_class_provider_method_filter_configuration_and_native_final_positions()
    {
        await using var app = Create();
        app.MapMediumApiNamespace<OrderedGroupDefinition>(group => group.WithMetadata(new EndpointResponseAttribute(412))).MapMediumApiEndpoint<OrderedEndpoint>();
        var route = Route(app, "/scope/ordered/value");
        Assert.Equal(Enumerable.Range(410, 11), Declared(route).Select(response => response.StatusCode));
        Assert.Equal(typeof(OrderedEndpoint), route.Metadata.GetMetadata<MethodInfo>()?.DeclaringType);
        Assert.Equal(typeof(OrderedEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/ordered/value");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("configured", Assert.Single(response.Headers.GetValues("X-Scope-Configured")));
        Assert.Equal("declared", Assert.Single(response.Headers.GetValues("X-Scope-Declared")));
    }

    [Fact]
    public async Task Direct_class_ownership_ignores_base_declarations_but_retains_inherited_handler_attributes()
    {
        await using var app = Create();
        app.MapMediumApiNamespace<InheritedGroupDefinition>().MapMediumApiEndpoint<InheritedEndpoint>();
        var route = Route(app, "/scope/inherited/value");
        Assert.Equal(new[] { 441, 442, 441 }, Declared(route).Select(response => response.StatusCode));
        Assert.DoesNotContain(Declared(route), response => response.StatusCode == 440);
        Assert.Equal(typeof(EndpointBase), route.Metadata.GetMetadata<MethodInfo>()?.DeclaringType);
        Assert.Equal(typeof(InheritedEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType);
        var success = Assert.Single(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), metadata => metadata.StatusCode == 200);
        Assert.Equal(typeof(ScopePayload), success.Type);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new ScopePayload("inherited"), await client.GetFromJsonAsync<ScopePayload>("/scope/inherited/value"));
    }

    [Fact]
    public async Task Class_declarations_retain_the_same_status_type_and_media_defaults_as_native_Produces()
    {
        await using var app = Create();
        app.MapMediumApiEndpoint<FormatsEndpoint>();
        app.MapGet("/scope/native-formats", NativeNoContent).WithName("NativeFormats").Produces(461).Produces<ScopePayload>(462).Produces<string>(463, "text/plain", "application/json");
        var declared = Route(app, "/scope/formats").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Where(value => value.StatusCode is 461 or 462 or 463).Select(Value).ToArray();
        var native = Route(app, "/scope/native-formats").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Where(value => value.StatusCode is 461 or 462 or 463).Select(Value).ToArray();
        Assert.Equal(native, declared);
        Assert.Equal(typeof(void), Assert.Single(Declared(Route(app, "/scope/formats")), value => value.StatusCode == 461).Type);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/formats");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public void Attribute_targets_allow_repeated_class_and_method_declarations_without_exposing_mutable_media_collections()
    {
        var usage = typeof(EndpointResponseAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        Assert.Equal(AttributeTargets.Class | AttributeTargets.Method, usage.ValidOn);
        Assert.True(usage.AllowMultiple);
        Assert.True(usage.Inherited);
        Assert.False(typeof(EndpointResponseAttribute).IsSealed);
        var additional = new[] { "application/json" };
        var declaration = new EndpointResponseAttribute(463, typeof(string), "text/plain", additional);
        additional[0] = "application/xml";
        Assert.Equal(new[] { "text/plain", "application/json" }, declaration.ContentTypes);
        var exposed = Assert.IsAssignableFrom<IList<string>>(declaration.ContentTypes);
        Assert.Throws<NotSupportedException>(() => exposed[0] = "application/xml");
        Assert.Throws<NotSupportedException>(exposed.Clear);
        Assert.Null(typeof(EndpointResponseAttribute).GetProperty(nameof(EndpointResponseAttribute.StatusCode))!.SetMethod);
        Assert.Equal(new[] { "application/json" }, new EndpointResponseAttribute(462, typeof(ScopePayload), "").ContentTypes);
        Assert.Empty(new EndpointResponseAttribute(461).ContentTypes);
    }

    [Fact]
    public async Task Class_readers_and_filter_registration_do_not_construct_unrelated_response_metadata_attributes()
    {
        await using var app = Create();
        app.MapMediumApiNamespace<IgnoredGroupDefinition>().MapMediumApiEndpoint<IgnoredEndpoint>();
        var route = Route(app, "/scope/ignored/value");
        Assert.Equal(new[] { 470, 471, 487 }, Declared(route).Select(response => response.StatusCode));
        Assert.DoesNotContain(route.Metadata, value => value is UnrelatedResponseAttribute);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/ignored/value");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, Probe(app).FilterCalls);
    }

    [Fact]
    public async Task Independent_hosts_and_registrations_receive_fresh_metadata_without_constructing_request_targets()
    {
        await using var repeated = Create();
        await using var independent = Create();
        repeated.MapGroup("/first").MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>().WithName("FirstFresh");
        repeated.MapGroup("/second").MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>().WithName("SecondFresh");
        independent.MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>();
        var materialized = Routes(repeated).ToArray();
        var first = Declared(Assert.Single(materialized, route => route.RoutePattern.RawText == "/first/scope/fresh/value")).ToArray();
        var second = Declared(Assert.Single(materialized, route => route.RoutePattern.RawText == "/second/scope/fresh/value")).ToArray();
        var other = Declared(Route(independent, "/scope/fresh/value")).ToArray();
        Assert.Equal(new[] { 480, 481, 487 }, first.Select(value => value.StatusCode));
        for (var index = 0; index < first.Length; index++)
        {
            Assert.NotSame(first[index], second[index]);
            Assert.NotSame(first[index], other[index]);
            Assert.Equal(first[index].ContentTypes, second[index].ContentTypes);
            if (first[index].ContentTypes.Any())
            {
                Assert.NotSame(first[index].ContentTypes, second[index].ContentTypes);
            }
        }

        Assert.Equal(2, repeated.GetMediumApiRouteReport().Routes.Count);
        Assert.Equal(2, repeated.GetMediumApiContractSnapshot().Routes.Count);
        await repeated.StartAsync();
        Assert.Equal(2, Descriptions(repeated).Operations.Count);
        // FreshEndpoint's constructor throws; inspection and startup must not invoke it.
    }

    [Fact]
    public async Task Plain_filter_class_responses_are_applied_only_through_MediumAPI_registration_not_native_registration()
    {
        await using var app = Create();
        app.MapGet("/scope/medium-filter", NativeNoContent).WithName("MediumFilter").AddMediumApiFilter<PlainResponseFilter>();
        app.MapGet("/scope/native-filter", NativeNoContent).WithName("NativeFilter").AddEndpointFilter<PlainResponseFilter>();
        Assert.Equal(new[] { 487 }, Declared(Route(app, "/scope/medium-filter")).Select(value => value.StatusCode));
        Assert.Empty(Declared(Route(app, "/scope/native-filter")));
        Assert.DoesNotContain(Declared(Route(app, "/scope/medium-filter")), value => value.StatusCode == 488);
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/scope/medium-filter", "/scope/native-filter" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        Assert.Equal(2, Probe(app).FilterCalls);
        // PlainResponseFilter.Configure throws if called; it has no convention-aware interface.
    }

    [Fact]
    public async Task Repeated_filter_registrations_keep_repeated_configuration_responses_headers_and_native_filter_calls()
    {
        await using var app = Create();
        app.MapGet("/scope/repeated", NativeNoContent).WithName("RepeatedFilter").AddMediumApiFilter<RepeatedResponseFilter>().AddMediumApiFilter<RepeatedResponseFilter>();
        var route = Route(app, "/scope/repeated");
        Assert.Equal(new[] { 489, 490, 489, 490 }, Declared(route).Select(value => value.StatusCode));
        Assert.Equal(2, route.Metadata.GetOrderedMetadata<ScopeConfigureMarker>().Count);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/repeated");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("present", Assert.Single(response.Headers.GetValues("X-Scope-Repeated")));
        Assert.Equal(2, Probe(app).FilterCalls);
    }

    [Fact]
    public async Task Declaration_failures_leave_earlier_configuration_visible_and_explicit_configuration_exceptions_keep_identity()
    {
        Assert.Throws<ArgumentNullException>(() => new EndpointResponseAttribute(491, typeof(string), "text/plain", null!));
        await using var app = Create();
        var endpoint = app.MapGet("/scope/failed-filter", NativeNoContent).WithName("FailedFilter");
        var error = Record.Exception(() => endpoint.AddMediumApiFilter<MalformedResponseFilter>());
        Assert.NotNull(error);
        Assert.IsType<ArgumentNullException>(RootCause(error));
        Assert.Single(Route(app, "/scope/failed-filter").Metadata.GetOrderedMetadata<ScopeConfigureMarker>());
        var group = app.MapGroup("/scope/failed-group");
        var groupError = Record.Exception(() => group.AddMediumApiFilter<MalformedResponseFilter>());
        Assert.NotNull(groupError);
        Assert.IsType<ArgumentNullException>(RootCause(groupError));
        group.MapGet("/value", NativeNoContent).WithName("FailedGroupFilter");
        Assert.Single(Route(app, "/scope/failed-group/value").Metadata.GetOrderedMetadata<ScopeConfigureMarker>());
        for (var attempt = 0; attempt < 2; attempt++)
        {
            Assert.Same(ThrowingConfigureFilter.Failure, Assert.Throws<InvalidOperationException>(() => endpoint.AddMediumApiFilter<ThrowingConfigureFilter>()));
            Assert.Same(ThrowingConfigureFilter.Failure, Assert.Throws<InvalidOperationException>(() => group.AddMediumApiFilter<ThrowingConfigureFilter>()));
        }

        Assert.Empty(Declared(Route(app, "/scope/failed-filter")));
    }

    [Fact]
    public async Task Manifests_and_automatic_selection_do_not_construct_disabled_or_manually_unmapped_response_declarations()
    {
        await using var app = Create();
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<ManualMalformedEndpoint>());
        Assert.Contains(manifest.Endpoints, value => value.EndpointType == typeof(ManualMalformedEndpoint) && value.IsManuallyMapped);
        Assert.Contains(manifest.Endpoints, value => value.EndpointType == typeof(DisabledEndpoint));
        Assert.Contains(manifest.Endpoints, value => value.EndpointType == typeof(DisabledChildEndpoint));
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ManualMalformedEndpoint>());
        Assert.Empty(Routes(app));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/disabled");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Disabled_typed_mounts_skip_callbacks_child_conditions_and_invalid_response_construction()
    {
        await using var app = Create();
        var callback = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callback = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callback = true));
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<DisabledGroupDefinition>(_ => callback = true));
        Assert.False(callback);
        Assert.Empty(Routes(app));
    }

    [Fact]
    public async Task Explicit_aliases_keep_endpoint_responses_without_importing_namespace_responses_prefixes_or_conditions()
    {
        await using var app = Create();
        app.MapMediumApiNamespace<ScopedGroupDefinition>().MapMediumApiEndpoint<ScopedEndpoint>();
        app.MapMediumApiEndpoint<ScopedEndpoint>("/scope/alias").WithName("ScopedAlias");
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        app.MapMediumApiEndpoint<DisabledAliasEndpoint>("/scope/disabled-alias").WithName("DisabledAlias");
        Assert.Equal(new[] { 470, 471 }, Declared(Route(app, "/scope/scoped/value")).Select(value => value.StatusCode));
        Assert.Equal(new[] { 471 }, Declared(Route(app, "/scope/alias")).Select(value => value.StatusCode));
        Assert.Equal(new[] { 475 }, Declared(Route(app, "/scope/disabled-alias")).Select(value => value.StatusCode));
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/scope/scoped/value", "/scope/alias", "/scope/disabled-alias" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact]
    public async Task Direct_child_mounts_do_not_reconstruct_parent_response_declarations_or_prefixes()
    {
        await using var app = Create();
        app.MapMediumApiNamespace<ChildGroupDefinition>().MapMediumApiEndpoint<ChildEndpoint>();
        var route = Assert.Single(Routes(app));
        Assert.Equal("/scope-child/value", route.RoutePattern.RawText);
        Assert.Equal(new[] { 473, 474 }, Declared(route).Select(value => value.StatusCode));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope-child/value");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var parentPrefix = await client.GetAsync("/scope-parent/scope-child/value");
        Assert.Equal(HttpStatusCode.NotFound, parentPrefix.StatusCode);
    }

    [Fact]
    public async Task Repeated_status_metadata_keeps_native_raw_and_API_description_contracts_without_new_snapshot_fields()
    {
        await using var app = Create();
        await using var native = Create();
        app.MapMediumApiNamespace<ContractGroupDefinition>().MapMediumApiEndpoint<ContractEndpoint>();
        Func<Ok<ScopePayload>> handler = new ContractEndpoint().HandleAsync;
        native.MapGroup("/scope/contracts").WithMetadata(new EndpointResponseAttribute(409, typeof(ScopeError))).MapGroup("").WithMetadata(new EndpointResponseAttribute(409, typeof(ScopePayload))).MapGet("/value", handler).WithName("Contract").Produces<int>(409);
        await app.StartAsync();
        await native.StartAsync();
        var snapshot = app.GetMediumApiContractSnapshot();
        Assert.True(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), snapshot).IsEquivalent);
        Assert.True(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(app)).IsEquivalent);
        var responses = Route(app, "/scope/contracts/value").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        Assert.Equal(4, responses.Count(value => value.StatusCode == 409));
        Assert.Single(responses, value => value.StatusCode == 200 && value.Type == typeof(ScopePayload));
        using var json = JsonDocument.Parse(snapshot.ToJson());
        Assert.Equal(new[] { "accepts", "allowsAnonymous", "authorization", "description", "httpMethods", "name", "order", "pattern", "requiresAntiforgery", "responses", "summary", "tags" }, json.RootElement.GetProperty("routes")[0].EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal));
        using var client = app.GetTestClient();
        Assert.Equal(new ScopePayload("contract"), await client.GetFromJsonAsync<ScopePayload>("/scope/contracts/value"));
    }

    [Fact]
    public async Task Response_declarations_do_not_execute_filters_or_change_typed_handler_status_payload_or_serialization()
    {
        await using var app = Create();
        app.MapMediumApiEndpoint<FilterCapableEndpoint>();
        var route = Route(app, "/scope/metadata-only");
        Assert.Equal(new[] { 418 }, Declared(route).Select(value => value.StatusCode));
        Assert.Single(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), value => value.StatusCode == 200 && value.Type == typeof(ScopePayload));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/metadata-only");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new ScopePayload("actual-success"), await response.Content.ReadFromJsonAsync<ScopePayload>());
        Assert.Equal(1, Probe(app).HandlerCalls);
        Assert.Equal(0, Probe(app).FilterCalls);
    }

    [Fact]
    public async Task Filter_class_response_declarations_also_apply_to_native_routes_registered_on_a_group()
    {
        await using var app = Create();
        var group = app.MapGroup("/scope/group-filter").AddMediumApiFilter<PlainResponseFilter>();
        group.MapGet("/first", NativeNoContent).WithName("GroupFirst");
        group.MapGet("/second", NativeNoContent).WithName("GroupSecond");
        Assert.All(Routes(app), route => Assert.Equal(new[] { 487 }, Declared(route).Select(value => value.StatusCode)));
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/scope/group-filter/first", "/scope/group-filter/second" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        Assert.Equal(2, Probe(app).FilterCalls);
    }

    private static WebApplication Create()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], ApplicationName = typeof(SharedResponsesScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<ScopeProbe>();
        return builder.Build();
    }

    private static ScopeProbe Probe(WebApplication app) => app.Services.GetRequiredService<ScopeProbe>();
    private static NoContent NativeNoContent() => TypedResults.NoContent();
    private static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    private static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    private static IEnumerable<EndpointResponseAttribute> Declared(RouteEndpoint endpoint) => endpoint.Metadata.GetOrderedMetadata<EndpointResponseAttribute>();
    private static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    private static string Value(IProducesResponseTypeMetadata metadata) => string.Join("|", metadata.StatusCode, metadata.Type?.FullName, string.Join(",", metadata.ContentTypes));

    private static Exception RootCause(Exception exception)
    {
        while (exception is TargetInvocationException { InnerException: not null } reflection)
        {
            exception = reflection.InnerException!;
        }

        return exception;
    }
}