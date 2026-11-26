using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.ScopeFixtures.Custom;
using RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.ScopeFixtures.Disabled;
using RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.ScopeFixtures.Disabled.Child;
using RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.ScopeFixtures.Inheritance;
using RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.ScopeFixtures.Ordered;
using RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.ScopeFixtures.Scoped;
using Xunit;
using static RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.EndpointAuthorizationHost;

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest;

public sealed class EndpointAuthorizationScopeTests
{
    [Fact]
    public async Task Class_metadata_is_between_group_conventions_and_inferred_method_and_generated_conventions()
    {
        await using var native = Create(mapEndpoints: false);
        await using var generated = Create(mapEndpoints: false);
        var nativeGroup = native.MapGroup("/scope/ordered").RequireAuthorization(SamplePolicies.GroupAccess);
        nativeGroup.WithMetadata(new AuthorizeAttribute(SamplePolicies.Readers));
        nativeGroup.MapGet("/value", NativeOrdered).RequireAuthorization(SamplePolicies.Readers).RequireAuthorization(SamplePolicies.Audit);
        generated.MapMediumApiNamespace<OrderedGroupDefinition>().MapMediumApiEndpoint<OrderedEndpoint>();
        var expected = new[] { SamplePolicies.GroupAccess, SamplePolicies.Readers, SamplePolicies.Audit, SamplePolicies.Writers, SamplePolicies.Readers, SamplePolicies.Audit };
        Assert.Equal(expected, Policies(Route(native, "/scope/ordered/value")));
        Assert.Equal(expected, Policies(Route(generated, "/scope/ordered/value")));
        Assert.Equal(typeof(OrderedEndpoint), Route(generated, "/scope/ordered/value").Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
    }

    [Fact]
    public async Task Inherited_original_handler_keeps_method_authentication_and_reads_only_the_concrete_endpoint_class()
    {
        await using var app = Create(mapEndpoints: false, includeSignIn: true);
        app.MapMediumApiEndpoint<DerivedEndpoint>();
        Assert.Equal(new[] { SamplePolicies.Writers, SamplePolicies.Readers }, Policies(Route(app, "/scope/inherited")));
        Assert.Equal(typeof(BaseEndpoint), Route(app, "/scope/inherited").Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        var reader = await Issue(client, "reader");
        using var rejected = await Send(client, "/scope/inherited", reader);
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
        var readerWriter = await Issue(client, "reader-writer");
        Assert.Equal(new AuthorizationReceipt("inherited", true), await Read(client, "/scope/inherited", readerWriter));
    }

    [Fact]
    public async Task Base_class_authorization_is_not_imported_when_an_endpoint_inherits_its_handler()
    {
        await using var app = Create(mapEndpoints: false, includeSignIn: true);
        app.MapMediumApiEndpoint<BaseClassIgnoredEndpoint>();
        Assert.Equal(new[] { SamplePolicies.Readers }, Policies(Route(app, "/scope/base-class-ignored")));
        await app.StartAsync();
        using var client = app.GetTestClient();
        var reader = await Issue(client, "reader");
        Assert.Equal(new AuthorizationReceipt("inherited", true), await Read(client, "/scope/base-class-ignored", reader));
    }

    [Fact]
    public async Task Supported_custom_class_metadata_preserves_constructor_named_fields_properties_and_native_contracts()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<CustomMetadataEndpoint>();
        var metadata = Route(app, "/scope/custom").Metadata;
        var policy = Assert.Single(metadata.GetOrderedMetadata<IAuthorizeData>());
        var custom = Assert.IsType<ProbeAuthorizeAttribute>(policy);
        Assert.Equal(ScopeProbeKind.Constructor, custom.Kind);
        Assert.Equal(new[] { "first", "second" }, custom.Labels);
        Assert.Equal(ScopeProbeKind.Named, custom.NamedKind);
        Assert.Equal(new[] { "named" }, custom.NamedLabels);
        Assert.Equal(SamplePolicies.Readers, custom.Policy);
        Assert.Equal("Reader", custom.Roles);
        Assert.Equal(SamplePolicies.PrimaryScheme, custom.AuthenticationSchemes);
        Assert.DoesNotContain(metadata, value => value is UnrelatedMvcFilterAttribute);
    }

    [Fact]
    public async Task Repeated_mounts_and_independent_hosts_receive_fresh_custom_attribute_objects_and_arrays()
    {
        await using var repeated = Create(mapEndpoints: false);
        await using var independent = Create(mapEndpoints: false);
        repeated.MapMediumApiEndpoint<CustomMetadataEndpoint>("/scope/first").WithName("ScopeFirst");
        repeated.MapMediumApiEndpoint<CustomMetadataEndpoint>("/scope/second").WithName("ScopeSecond");
        independent.MapMediumApiEndpoint<CustomMetadataEndpoint>();
        var first = Assert.Single(Route(repeated, "/scope/first").Metadata.OfType<ProbeAuthorizeAttribute>());
        var second = Assert.Single(Route(repeated, "/scope/second").Metadata.OfType<ProbeAuthorizeAttribute>());
        var otherHost = Assert.Single(Route(independent, "/scope/custom").Metadata.OfType<ProbeAuthorizeAttribute>());
        Assert.NotSame(first, second);
        Assert.NotSame(first, otherHost);
        Assert.NotSame(second, otherHost);
        Assert.NotSame(first.Labels, second.Labels);
        Assert.NotSame(first.NamedLabels, second.NamedLabels);
        first.Labels[0] = "changed";
        first.NamedLabels[0] = "changed";
        first.Policy = SamplePolicies.Writers;
        Assert.Equal("first", second.Labels[0]);
        Assert.Equal("first", otherHost.Labels[0]);
        Assert.Equal("named", second.NamedLabels[0]);
        Assert.Equal("named", otherHost.NamedLabels[0]);
        Assert.Equal(SamplePolicies.Readers, second.Policy);
        Assert.Equal(SamplePolicies.Readers, otherHost.Policy);
    }

    [Fact]
    public async Task Metadata_inspection_and_authorization_rejection_do_not_construct_the_endpoint_or_unrelated_attributes()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<CustomMetadataEndpoint>();
        Assert.Single(Route(app, "/scope/custom").Metadata.OfType<ProbeAuthorizeAttribute>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await Send(client, "/scope/custom");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_custom_allow_anonymous_contract_has_the_normal_native_bypass_effect()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapGroup("/parent").RequireAuthorization(SamplePolicies.GroupAccess).MapMediumApiEndpoint<CustomAnonymousEndpoint>();
        var metadata = Route(app, "/parent/scope/custom-anonymous").Metadata;
        Assert.Equal(new[] { SamplePolicies.GroupAccess }, metadata.GetOrderedMetadata<IAuthorizeData>().Select(value => value.Policy));
        var anonymous = Assert.IsType<ProbeAnonymousAttribute>(metadata.GetMetadata<IAllowAnonymous>());
        Assert.Equal(new[] { "public" }, anonymous.Labels);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new AuthorizationReceipt("custom-anonymous", false), await Read(client, "/parent/scope/custom-anonymous"));
    }

    [Fact]
    public async Task Supported_attribute_constructor_failures_propagate_when_native_endpoints_are_materialized()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<ThrowingMetadataEndpoint>();
        var error = Assert.Throws<TargetInvocationException>(() => Routes(app).ToArray());
        Assert.Equal("Selected authorization attribute construction failed.", Assert.IsType<InvalidOperationException>(error.InnerException).Message);
    }

    [Fact]
    public async Task Declaration_manifests_and_disabled_or_manual_automatic_mappings_do_not_construct_class_authorization()
    {
        await using var app = Create(mapEndpoints: false);
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<ThrowingMetadataEndpoint>());
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(ThrowingMetadataEndpoint) && endpoint.IsManuallyMapped);
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(DisabledEndpoint));
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(DisabledChildEndpoint));
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ThrowingMetadataEndpoint>());
        Assert.Empty(Routes(app));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/disabled");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Disabled_typed_mounts_skip_class_authorization_and_descendant_conditions()
    {
        await using var app = Create(mapEndpoints: false);
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callbackInvoked = true));
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        Assert.Empty(Routes(app));
    }

    [Fact]
    public async Task Explicit_individual_aliases_keep_class_policies_without_implicitly_importing_namespace_policies()
    {
        await using var app = Create(mapEndpoints: false, includeSignIn: true);
        app.MapMediumApiNamespace<ScopedGroupDefinition>().MapMediumApiEndpoint<ScopedEndpoint>();
        app.MapMediumApiEndpoint<ScopedEndpoint>("/scope/alias").WithName("ScopeAlias");
        Assert.Equal(new[] { SamplePolicies.GroupAccess, SamplePolicies.Readers }, Policies(Route(app, "/scope/scoped/value")));
        Assert.Equal(new[] { SamplePolicies.Readers }, Policies(Route(app, "/scope/alias")));
        await app.StartAsync();
        using var client = app.GetTestClient();
        var reader = await Issue(client, "reader");
        Assert.Equal(new AuthorizationReceipt("scoped", true), await Read(client, "/scope/alias", reader));
        using var rejected = await Send(client, "/scope/scoped/value", reader);
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
        var complete = await Issue(client, "complete");
        Assert.Equal(new AuthorizationReceipt("scoped", true), await Read(client, "/scope/scoped/value", complete));
    }

    [Fact]
    public async Task Explicit_aliases_bypass_namespace_conditions_but_still_require_declared_class_authorization()
    {
        await using var app = Create(mapEndpoints: false, includeSignIn: true);
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        app.MapMediumApiEndpoint<DisabledAliasEndpoint>("/scope/explicit-disabled-alias").WithName("ScopeExplicitDisabledAlias");
        Assert.Equal(new[] { SamplePolicies.Readers }, Policies(Route(app, "/scope/explicit-disabled-alias")));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var anonymous = await Send(client, "/scope/explicit-disabled-alias");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        var reader = await Issue(client, "reader");
        Assert.Equal(new AuthorizationReceipt("disabled-alias", true), await Read(client, "/scope/explicit-disabled-alias", reader));
    }

    private static string?[] Policies(RouteEndpoint endpoint) => endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(value => value.Policy).ToArray();

    [Authorize(SamplePolicies.Writers)]
    private static ScopeMetadataResult NativeOrdered() => new();
}