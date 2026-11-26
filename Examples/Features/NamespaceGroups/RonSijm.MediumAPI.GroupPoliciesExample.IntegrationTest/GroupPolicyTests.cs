using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest.Fixtures;
using RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest.Fixtures.ClassInheritance;
using RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest.Fixtures.Disabled;
using RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest.Fixtures.Disabled.Child;
using RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest.Fixtures.Probe;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.GroupPoliciesExample.Program;

namespace RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest;

public sealed class GroupPolicyTests
{
    [Theory]
    [InlineData("/api/status")]
    [InlineData("/api/native-status")]
    [InlineData("/api/reports/current")]
    public async Task Parent_policy_protects_own_and_descendant_endpoints(string path)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var anonymous = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.IdentityHeader, "member");
        using var forbidden = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.IdentityHeader);
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.IdentityHeader, "reader");
        using var allowed = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Anonymous_child_bypasses_parent_authorization_and_keeps_its_CORS_policy()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/public/status");
        request.Headers.Add("Origin", ExamplePolicies.AllowedOrigin);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ExamplePolicies.AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        var metadata = Endpoint(app, "/api/public/status").Metadata;
        Assert.Contains(metadata.GetOrderedMetadata<IAuthorizeData>(), data => data.Policy == ExamplePolicies.Readers);
        Assert.NotNull(metadata.GetMetadata<IAllowAnonymous>());
    }

    [Theory]
    [InlineData("/api/reports/current")]
    [InlineData("/api/native-status")]
    public async Task Named_CORS_metadata_handles_preflight_before_authentication(string path)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = Preflight(path);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(ExamplePolicies.AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        var endpoint = Endpoint(app, path);
        Assert.Equal(ExamplePolicies.Browser, Assert.IsType<EnableCorsAttribute>(endpoint.Metadata.GetMetadata<ICorsMetadata>()).PolicyName);
        Assert.True(endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.AcceptCorsPreflight);
    }

    [Fact]
    public async Task DisableCors_child_suppresses_inherited_CORS_headers_without_changing_anonymous_access()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/public/local-only/status");
        request.Headers.Add("Origin", ExamplePolicies.AllowedOrigin);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        using var preflight = Preflight("/api/public/local-only/status");
        using var preflightResponse = await client.SendAsync(preflight);
        Assert.Equal(HttpStatusCode.NoContent, preflightResponse.StatusCode);
        Assert.False(preflightResponse.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.IsType<DisableCorsAttribute>(Endpoint(app, "/api/public/local-only/status").Metadata.GetMetadata<ICorsMetadata>());
    }

    [Fact]
    public async Task Independent_typed_maps_get_fresh_metadata_in_framework_convention_order()
    {
        await using var first = CreateHost();
        await using var second = CreateHost();
        first.MapMediumApiNamespace<ProbeGroupDefinition>(group => group.WithMetadata(new OrderMarker("Host")));
        second.MapMediumApiNamespace<ProbeGroupDefinition>(group => group.WithMetadata(new OrderMarker("Host")));
        var firstEndpoint = Endpoint(first, "/policy-probe/value");
        var secondEndpoint = Endpoint(second, "/policy-probe/value");
        var firstPolicy = Assert.Single(firstEndpoint.Metadata.OfType<ProbeAuthorizeAttribute>());
        var secondPolicy = Assert.Single(secondEndpoint.Metadata.OfType<ProbeAuthorizeAttribute>());
        Assert.NotSame(firstPolicy, secondPolicy);
        Assert.NotEqual(firstPolicy.InstanceId, secondPolicy.InstanceId);
        Assert.NotSame(firstEndpoint.Metadata.GetMetadata<AuthorizeAttribute>(), secondEndpoint.Metadata.GetMetadata<AuthorizeAttribute>());
        Assert.NotSame(firstEndpoint.Metadata.GetMetadata<EnableCorsAttribute>(), secondEndpoint.Metadata.GetMetadata<EnableCorsAttribute>());
        Assert.Equal(ProbeKind.Constructor, firstPolicy.Kind);
        Assert.Equal(ProbeKind.Named, firstPolicy.NamedKind);
        Assert.Equal(new[] { "first", "second" }, firstPolicy.Labels);
        Assert.Equal(ExamplePolicies.Readers, firstPolicy.Policy);
        firstPolicy.Labels[0] = "first host mutation";
        Assert.Equal("first", secondPolicy.Labels[0]);
        Assert.Null(firstEndpoint.Metadata.GetMetadata<EnableCorsAttribute>()!.PolicyName);
        Assert.DoesNotContain(firstEndpoint.Metadata, metadata => metadata is IAsyncActionFilter);
        var ordered = firstEndpoint.Metadata.ToList();
        Assert.True(ordered.FindIndex(metadata => metadata is OrderMarker { Stage: "Map" }) < ordered.IndexOf(firstPolicy));
        Assert.True(ordered.IndexOf(firstPolicy) < ordered.FindIndex(metadata => metadata is OrderMarker { Stage: "Host" }));
        Assert.True(ordered.FindIndex(metadata => metadata is OrderMarker { Stage: "Host" }) < ordered.FindIndex(metadata => metadata is OrderMarker { Stage: "Endpoint" }));
    }

    [Fact]
    public async Task Default_CORS_attribute_uses_the_hosts_default_policy()
    {
        await using var app = CreateHost();
        app.MapMediumApiNamespace<ProbeGroupDefinition>();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = Preflight("/policy-probe/value");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(ExamplePolicies.AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Disabled_parent_skips_policy_constructors_Map_callbacks_and_child_conditions()
    {
        await using var app = CreateHost();
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<DisabledGroupDefinition>());
        Assert.Contains(manifest.NamespaceGroups, definition => definition.GroupType == typeof(DisabledGroupDefinition));
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<DisabledGroupDefinition>());
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callbackInvoked = true));
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var parent = await client.GetAsync("/disabled-parent");
        using var child = await client.GetAsync("/disabled-child");
        Assert.Equal(HttpStatusCode.NotFound, parent.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, child.StatusCode);
        Assert.DoesNotContain(Endpoints(app), endpoint => endpoint.RoutePattern.RawText?.Contains("disabled", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Base_class_attributes_are_ignored_and_definition_instances_are_not_created()
    {
        await using var app = CreateHost();
        app.MapMediumApiNamespace<DerivedGroupDefinition>();
        var endpoint = Endpoint(app, "/class-inheritance/value");
        Assert.Empty(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
        Assert.Null(endpoint.Metadata.GetMetadata<ICorsMetadata>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/class-inheritance/value");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static WebApplication CreateHost() => ExampleProgram.Build(configure: builder =>
    {
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
            options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
            options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
        }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
    });

    private static HttpRequestMessage Preflight(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.Add("Origin", ExamplePolicies.AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return request;
    }

    private static IEnumerable<RouteEndpoint> Endpoints(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    private static RouteEndpoint Endpoint(WebApplication app, string pattern) => Assert.Single(Endpoints(app), endpoint => endpoint.RoutePattern.RawText == pattern);
}