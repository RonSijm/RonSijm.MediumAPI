using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures;
using RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Composed.Child;
using RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Disabled;
using RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Disabled.Child;
using RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Fresh;
using RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Inherited;
using RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Manual;
using RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Scoped;
using Xunit;
using static RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.EndpointIdentityHost;

namespace RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest;

public sealed class EndpointIdentityScopeTests
{
    [Fact]
    public void Marker_has_a_sealed_immutable_public_surface_and_no_public_construction_API()
    {
        var type = typeof(MediumApiEndpointMetadata);
        Assert.True(type.IsPublic);
        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        var constructor = Assert.Single(type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance));
        Assert.True(constructor.IsAssembly);
        Assert.Equal(typeof(Type), Assert.Single(constructor.GetParameters()).ParameterType);
        var property = Assert.Single(type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal(nameof(MediumApiEndpointMetadata.EndpointType), property.Name);
        Assert.Equal(typeof(Type), property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
    }

    [Fact]
    public async Task Inferred_providers_observe_concrete_identity_before_inference_without_replacing_inherited_MethodInfo()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<ProviderEndpoint>();
        var route = Route(app, "/scope/provider");
        Assert.Equal(typeof(ProviderEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        var observation = Assert.Single(route.Metadata.GetOrderedMetadata<ScopeProviderObservation>());
        Assert.Equal(typeof(ProviderEndpoint), observation.EndpointType);
        Assert.Equal(typeof(ProviderHandlerBase), observation.HandlerDeclaringType);
        var method = route.Metadata.GetMetadata<MethodInfo>();
        Assert.Equal(typeof(ProviderHandlerBase), method!.DeclaringType);
        Assert.Equal(nameof(ProviderHandlerBase.HandleAsync), method.Name);
        var report = Assert.Single(app.GetMediumApiRouteReport().Routes);
        Assert.Equal(typeof(ProviderEndpoint), report.EndpointType);
        Assert.Equal(typeof(ProviderHandlerBase), report.HandlerDeclaringType);
        Assert.Equal(nameof(ProviderHandlerBase.HandleAsync), report.HandlerName);
    }

    [Fact]
    public async Task Aliases_independent_hosts_and_concrete_classes_sharing_one_handler_receive_fresh_markers()
    {
        await using var repeated = Create(mapEndpoints: false);
        await using var independent = Create(mapEndpoints: false);
        repeated.MapMediumApiEndpoint<FreshEndpoint>("/scope/first").WithName("FirstFresh");
        repeated.MapMediumApiEndpoint<FreshEndpoint>("/scope/second").WithName("SecondFresh");
        repeated.MapMediumApiEndpoint<OtherFreshEndpoint>();
        independent.MapMediumApiEndpoint<FreshEndpoint>();
        var first = Assert.Single(Route(repeated, "/scope/first").Metadata.GetOrderedMetadata<MediumApiEndpointMetadata>());
        var second = Assert.Single(Route(repeated, "/scope/second").Metadata.GetOrderedMetadata<MediumApiEndpointMetadata>());
        var otherType = Assert.Single(Route(repeated, "/scope/other-fresh").Metadata.GetOrderedMetadata<MediumApiEndpointMetadata>());
        var otherHost = Assert.Single(Route(independent, "/scope/fresh").Metadata.GetOrderedMetadata<MediumApiEndpointMetadata>());
        Assert.Equal(typeof(FreshEndpoint), first.EndpointType);
        Assert.Equal(typeof(FreshEndpoint), second.EndpointType);
        Assert.Equal(typeof(FreshEndpoint), otherHost.EndpointType);
        Assert.Equal(typeof(OtherFreshEndpoint), otherType.EndpointType);
        Assert.NotSame(first, second);
        Assert.NotSame(first, otherHost);
        Assert.NotSame(first, otherType);
        Assert.Equal(typeof(SharedHandlerBase), Route(repeated, "/scope/other-fresh").Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
    }

    [Fact]
    public async Task Routes_reports_and_documentation_capture_do_not_construct_endpoint_targets()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<FreshEndpoint>();
        Assert.Equal(typeof(FreshEndpoint), Assert.Single(Routes(app)).Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        Assert.Equal(typeof(FreshEndpoint), Assert.Single(app.GetMediumApiRouteReport().Routes).EndpointType);
        Assert.Single(app.GetMediumApiContractSnapshot().Routes);
        await app.StartAsync();
        Assert.Single(ApiDescriptions(app).Operations);
        Assert.Equal(typeof(FreshEndpoint), Assert.Single(app.GetMediumApiRouteReport().Routes).EndpointType);
        // FreshEndpoint's constructor throws: all of these operations must remain startup inspection only.
    }

    [Fact]
    public async Task Manifest_and_automatic_selection_skip_manual_and_disabled_targets_without_materializing_marker_metadata()
    {
        await using var app = Create(mapEndpoints: false);
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Contains(manifest.Endpoints, item => item.EndpointType == typeof(ManualThrowingEndpoint) && item.IsManuallyMapped);
        Assert.Contains(manifest.Endpoints, item => item.EndpointType == typeof(DisabledEndpoint));
        Assert.Contains(manifest.Endpoints, item => item.EndpointType == typeof(DisabledChildEndpoint));
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Empty(Routes(app));
        Assert.Empty(app.GetMediumApiRouteReport().Routes);
        Assert.Empty(app.GetMediumApiContractSnapshot().Routes);
        await app.StartAsync();
        Assert.Empty(ApiDescriptions(app).Operations);
    }

    [Fact]
    public async Task Disabled_typed_namespace_selection_skips_callbacks_descendant_conditions_metadata_and_targets()
    {
        await using var app = Create(mapEndpoints: false);
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callbackInvoked = true));
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        Assert.Empty(Routes(app));
        Assert.Empty(app.GetMediumApiRouteReport().Routes);
    }

    [Fact]
    public async Task Explicit_alias_identity_does_not_import_namespace_hosts_prefixes_or_disabled_ancestor_conditions()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ScopedGroupDefinition>().MapMediumApiEndpoint<ScopedEndpoint>();
        app.MapMediumApiEndpoint<ScopedEndpoint>("/scope/alias").WithName("ScopedAlias");
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        app.MapMediumApiEndpoint<DisabledAliasEndpoint>("/scope/disabled-alias").WithName("DirectDisabledAlias");
        Assert.Equal(typeof(ScopedEndpoint), Route(app, "/scope/scoped/value").Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        Assert.Equal(typeof(ScopedEndpoint), Route(app, "/scope/alias").Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        Assert.Equal("group.scope.test", Assert.Single(Route(app, "/scope/scoped/value").Metadata.GetMetadata<IHostMetadata>()!.Hosts));
        Assert.Null(Route(app, "/scope/alias").Metadata.GetMetadata<IHostMetadata>());
        Assert.Null(Route(app, "/scope/disabled-alias").Metadata.GetMetadata<IHostMetadata>());
        Assert.Equal(typeof(DisabledAliasEndpoint), Route(app, "/scope/disabled-alias").Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var groupedRequest = new HttpRequestMessage(HttpMethod.Get, "/scope/scoped/value");
        groupedRequest.Headers.Host = "group.scope.test";
        using var groupedResponse = await client.SendAsync(groupedRequest);
        Assert.Equal(HttpStatusCode.OK, groupedResponse.StatusCode);
        Assert.Equal(new ScopeResponse("scoped"), await groupedResponse.Content.ReadFromJsonAsync<ScopeResponse>());
        Assert.Equal(new ScopeResponse("scoped"), await client.GetFromJsonAsync<ScopeResponse>("/scope/alias"));
        Assert.Equal(new ScopeResponse("disabled-alias"), await client.GetFromJsonAsync<ScopeResponse>("/scope/disabled-alias"));
    }

    [Fact]
    public async Task Direct_child_namespace_mount_keeps_concrete_identity_without_reconstructing_ancestor_policies()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ChildGroupDefinition>().MapMediumApiEndpoint<ChildEndpoint>();
        var route = Assert.Single(Routes(app));
        Assert.Equal("/scope-child/value", route.RoutePattern.RawText);
        Assert.Equal(typeof(ChildEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        Assert.Null(route.Metadata.GetMetadata<IHostMetadata>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new ScopeResponse("child"), await client.GetFromJsonAsync<ScopeResponse>("/scope-child/value"));
        using var ancestorPrefix = await client.GetAsync("/scope-parent/scope-child/value");
        Assert.Equal(HttpStatusCode.NotFound, ancestorPrefix.StatusCode);
    }

    [Fact]
    public async Task Native_bound_IEndpoint_handlers_keep_old_declaring_type_fallback_while_ordinary_lambdas_stay_unidentified()
    {
        await using var app = Create(mapEndpoints: false);
        var target = new LegacyEndpoint();
        app.MapGet("/scope/native-legacy", target.HandleAsync).WithName("NativeLegacy");
        app.MapGet("/scope/native-ordinary", () => TypedResults.Ok(new ScopeResponse("ordinary"))).WithName("NativeOrdinary");
        app.MapMediumApiEndpoint<LegacyEndpoint>("/scope/modern-legacy");
        Assert.Null(Route(app, "/scope/native-legacy").Metadata.GetMetadata<MediumApiEndpointMetadata>());
        Assert.Null(Route(app, "/scope/native-ordinary").Metadata.GetMetadata<MediumApiEndpointMetadata>());
        var report = app.GetMediumApiRouteReport();
        var native = Assert.Single(report.Routes, item => item.Pattern == "/scope/native-legacy");
        Assert.Equal(typeof(LegacyHandlerBase), native.EndpointType);
        Assert.Equal(typeof(LegacyHandlerBase), native.HandlerDeclaringType);
        Assert.Null(Assert.Single(report.Routes, item => item.Pattern == "/scope/native-ordinary").EndpointType);
        var modern = Assert.Single(report.Routes, item => item.Pattern == "/scope/modern-legacy");
        Assert.Equal(typeof(LegacyEndpoint), modern.EndpointType);
        Assert.Equal(typeof(LegacyHandlerBase), modern.HandlerDeclaringType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new ScopeResponse("legacy"), await client.GetFromJsonAsync<ScopeResponse>("/scope/native-legacy"));
        Assert.Equal(new ScopeResponse("legacy"), await client.GetFromJsonAsync<ScopeResponse>("/scope/modern-legacy"));
        Assert.Equal(new ScopeResponse("ordinary"), await client.GetFromJsonAsync<ScopeResponse>("/scope/native-ordinary"));
    }

    [Fact]
    public async Task Host_final_conventions_can_remove_identity_after_providers_and_restore_the_original_report_fallback()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<ProviderEndpoint>().Finally(builder =>
        {
            for (var index = builder.Metadata.Count - 1; index >= 0; index--)
            {
                if (builder.Metadata[index] is MediumApiEndpointMetadata)
                {
                    builder.Metadata.RemoveAt(index);
                }
            }
        });
        var route = Route(app, "/scope/provider");
        Assert.Null(route.Metadata.GetMetadata<MediumApiEndpointMetadata>());
        Assert.Equal(typeof(ProviderEndpoint), Assert.Single(route.Metadata.GetOrderedMetadata<ScopeProviderObservation>()).EndpointType);
        Assert.Equal(typeof(ProviderHandlerBase), route.Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        var report = Assert.Single(app.GetMediumApiRouteReport().Routes);
        Assert.Null(report.EndpointType);
        Assert.Equal(typeof(ProviderHandlerBase), report.HandlerDeclaringType);
    }

    [Fact]
    public async Task Captured_markers_follow_native_metadata_precedence_and_are_descriptive_rather_than_mapping_provenance()
    {
        await using var source = Create(mapEndpoints: false);
        source.MapMediumApiEndpoint<FreshEndpoint>();
        var captured = Route(source, "/scope/fresh").Metadata.GetMetadata<MediumApiEndpointMetadata>()!;
        await using var app = Create(mapEndpoints: false);
        app.MapGet("/scope/reused-native", () => TypedResults.Ok(new ScopeResponse("native"))).WithName("ReusedNative").WithMetadata(captured);
        app.MapGroup("/group").WithMetadata(captured).MapMediumApiEndpoint<OtherFreshEndpoint>().WithName("InheritedMarker");
        app.MapMediumApiEndpoint<OtherFreshEndpoint>("/scope/overridden").WithName("OverriddenMarker").WithMetadata(captured);
        var inherited = Route(app, "/group/scope/other-fresh").Metadata;
        Assert.Equal(2, inherited.GetOrderedMetadata<MediumApiEndpointMetadata>().Count);
        Assert.Equal(typeof(OtherFreshEndpoint), inherited.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        Assert.Same(captured, Route(app, "/scope/reused-native").Metadata.GetMetadata<MediumApiEndpointMetadata>());
        Assert.Same(captured, Route(app, "/scope/overridden").Metadata.GetMetadata<MediumApiEndpointMetadata>());
        Assert.Equal(typeof(SharedHandlerBase), Route(app, "/scope/overridden").Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        var report = app.GetMediumApiRouteReport();
        Assert.Equal(typeof(FreshEndpoint), Assert.Single(report.Routes, item => item.Pattern == "/scope/reused-native").EndpointType);
        Assert.Equal(typeof(OtherFreshEndpoint), Assert.Single(report.Routes, item => item.Pattern == "/group/scope/other-fresh").EndpointType);
        Assert.Equal(typeof(FreshEndpoint), Assert.Single(report.Routes, item => item.Pattern == "/scope/overridden").EndpointType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new ScopeResponse("native"), await client.GetFromJsonAsync<ScopeResponse>("/scope/reused-native"));
    }

    [Fact]
    public async Task Raw_and_API_Explorer_snapshots_keep_their_existing_shape_and_do_not_prove_concrete_identity()
    {
        await using var native = Create(mapEndpoints: false);
        await using var mapped = Create(mapEndpoints: false);
        native.MapGet("/scope/contract", NativeContract).WithName("ScopeContract");
        mapped.MapMediumApiEndpoint<FreshEndpoint>("/scope/contract").WithName("ScopeContract");
        Assert.Null(Assert.Single(native.GetMediumApiRouteReport().Routes).EndpointType);
        Assert.Equal(typeof(FreshEndpoint), Assert.Single(mapped.GetMediumApiRouteReport().Routes).EndpointType);
        Assert.Equal(native.GetMediumApiContractSnapshot().ToJson(), mapped.GetMediumApiContractSnapshot().ToJson());
        await native.StartAsync();
        await mapped.StartAsync();
        Assert.Equal(ApiDescriptions(native).ToJson(), ApiDescriptions(mapped).ToJson());
        Assert.DoesNotContain("endpointType", mapped.GetMediumApiContractSnapshot().ToJson(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(nameof(MediumApiEndpointMetadata), ApiDescriptions(mapped).ToJson(), StringComparison.Ordinal);
    }

    private static Ok<ScopeResponse> NativeContract() => TypedResults.Ok(new ScopeResponse("fresh"));
}