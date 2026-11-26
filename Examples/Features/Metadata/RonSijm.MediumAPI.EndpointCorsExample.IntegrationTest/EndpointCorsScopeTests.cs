using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.ScopeFixtures.Disabled;
using RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.ScopeFixtures.Disabled.Child;
using RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.ScopeFixtures.Exact;
using RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.ScopeFixtures.Inheritance;
using RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.ScopeFixtures.Ordered;
using RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.ScopeFixtures.Scoped;
using Xunit;
using static RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.EndpointCorsHost;

namespace RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest;

public sealed class EndpointCorsScopeTests
{
    [Fact]
    public async Task Class_metadata_is_between_group_and_native_provider_method_and_Configure_metadata()
    {
        await using var native = Create(mapEndpoints: false);
        await using var generated = Create(mapEndpoints: false);
        var group = native.MapGroup("/scope/ordered").RequireCors(SamplePolicies.Browser).WithMetadata(new DisableCorsAttribute());
        group.MapGet("/value", NativeOrdered).WithMetadata(new DisableCorsAttribute());
        generated.MapMediumApiNamespace<OrderedGroupDefinition>().MapMediumApiEndpoint<OrderedEndpoint>();
        var expected = new[] { "enable:" + SamplePolicies.Browser, "disable", "enable:" + SamplePolicies.Override, "enable:" + SamplePolicies.Browser, "disable" };
        Assert.Equal(expected, CorsKinds(Route(native, "/scope/ordered/value")));
        Assert.Equal(expected, CorsKinds(Route(generated, "/scope/ordered/value")));
        Assert.IsType<DisableCorsAttribute>(Route(generated, "/scope/ordered/value").Metadata.GetMetadata<ICorsMetadata>());
        Assert.True(Route(generated, "/scope/ordered/value").Metadata.GetMetadata<IHttpMethodMetadata>()!.AcceptCorsPreflight);
        Assert.Equal(typeof(OrderedEndpoint), Route(generated, "/scope/ordered/value").Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
    }

    [Fact]
    public async Task An_inherited_handler_keeps_method_metadata_without_importing_base_class_declarations()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<DerivedEndpoint>();
        app.MapMediumApiEndpoint<BaseClassIgnoredEndpoint>();
        Assert.Equal(new[] { "enable:" + SamplePolicies.Browser, "enable:" + SamplePolicies.Override }, CorsKinds(Route(app, "/scope/inherited")));
        Assert.Equal(new[] { "enable:" + SamplePolicies.Override }, CorsKinds(Route(app, "/scope/base-class-ignored")));
        Assert.Equal(typeof(BaseEndpoint), Route(app, "/scope/inherited").Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/scope/inherited", "/scope/base-class-ignored" })
        {
            using var response = await Send(client, path, SamplePolicies.OverrideOrigin);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(SamplePolicies.OverrideOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        }
    }

    [Fact]
    public async Task Derived_custom_and_unrelated_class_attributes_are_ignored_without_construction()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<IgnoredEndpoint>();
        var metadata = Route(app, "/scope/ignored").Metadata;
        Assert.Null(metadata.GetMetadata<ICorsMetadata>());
        Assert.DoesNotContain(metadata, value => value is DerivedCorsAttribute or CustomCorsAttribute or UnrelatedMvcAttribute);
        Assert.False(metadata.GetMetadata<IHttpMethodMetadata>()!.AcceptCorsPreflight);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await Send(client, "/scope/ignored");
        Assert.Equal(new ScopeReceipt("ignored"), await response.Content.ReadFromJsonAsync<ScopeReceipt>());
    }

    [Fact]
    public async Task An_existing_authorization_contract_remains_supported_even_when_it_also_implements_CORS_metadata()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<CompatibleAuthorizationEndpoint>();
        var metadata = Route(app, "/scope/compatible-authorization").Metadata;
        var authorization = Assert.Single(metadata.GetOrderedMetadata<IAuthorizeData>());
        var cors = Assert.IsType<CompatibleCorsAuthorizationAttribute>(metadata.GetMetadata<ICorsMetadata>());
        Assert.Same(authorization, cors);
        Assert.Equal("ScopePolicy", authorization.Policy);
        Assert.Equal(SamplePolicies.Browser, cors.PolicyName);
    }

    [Fact]
    public async Task Named_property_values_and_mutable_attribute_instances_are_fresh_per_mount_and_host()
    {
        await using var repeated = Create(mapEndpoints: false);
        await using var independent = Create(mapEndpoints: false);
        repeated.MapMediumApiEndpoint<SetterEndpoint>("/scope/first").WithName("ScopeFirst");
        repeated.MapMediumApiEndpoint<SetterEndpoint>("/scope/second").WithName("ScopeSecond");
        independent.MapMediumApiEndpoint<SetterEndpoint>();
        var first = Assert.IsType<EnableCorsAttribute>(Route(repeated, "/scope/first").Metadata.GetMetadata<ICorsMetadata>());
        var second = Assert.IsType<EnableCorsAttribute>(Route(repeated, "/scope/second").Metadata.GetMetadata<ICorsMetadata>());
        var otherHost = Assert.IsType<EnableCorsAttribute>(Route(independent, "/scope/setter").Metadata.GetMetadata<ICorsMetadata>());
        Assert.Equal(SamplePolicies.Browser, first.PolicyName);
        Assert.Equal(SamplePolicies.Browser, second.PolicyName);
        Assert.NotSame(first, second);
        Assert.NotSame(first, otherHost);
        Assert.NotSame(second, otherHost);
        first.PolicyName = SamplePolicies.Override;
        Assert.Equal(SamplePolicies.Browser, second.PolicyName);
        Assert.Equal(SamplePolicies.Browser, otherHost.PolicyName);
    }

    [Fact]
    public async Task Native_preflight_short_circuits_without_constructing_the_endpoint_instance()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<PreflightOnlyEndpoint>();
        Assert.True(Route(app, "/scope/preflight-only").Metadata.GetMetadata<IHttpMethodMetadata>()!.AcceptCorsPreflight);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await Preflight(client, "/scope/preflight-only", SamplePolicies.BrowserOrigin);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(SamplePolicies.BrowserOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Manifests_disabled_trees_and_automatic_manual_exclusion_do_not_materialize_class_metadata()
    {
        await using var app = Create(mapEndpoints: false);
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<DisabledEndpoint>());
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(DisabledEndpoint));
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(DisabledChildEndpoint));
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(PreflightOnlyEndpoint) && endpoint.IsManuallyMapped);
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<DisabledEndpoint>());
        Assert.Empty(Routes(app));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/disabled");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Explicit_aliases_keep_class_metadata_without_importing_the_namespace_parent_policy()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ScopedGroupDefinition>().MapMediumApiEndpoint<ScopedEndpoint>();
        app.MapMediumApiEndpoint<ScopedEndpoint>("/scope/alias").WithName("ScopeAlias");
        Assert.Equal(new[] { "enable:" + SamplePolicies.Override, "enable:" + SamplePolicies.Browser }, CorsKinds(Route(app, "/scope/scoped/value")));
        Assert.Equal(new[] { "enable:" + SamplePolicies.Browser }, CorsKinds(Route(app, "/scope/alias")));
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/scope/scoped/value", "/scope/alias" })
        {
            using var response = await Send(client, path, SamplePolicies.BrowserOrigin);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(SamplePolicies.BrowserOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        }
    }

    [Fact]
    public async Task Explicit_aliases_bypass_namespace_conditions_and_retain_the_class_CORS_policy()
    {
        await using var app = Create(mapEndpoints: false);
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        app.MapMediumApiEndpoint<DisabledAliasEndpoint>("/scope/explicit-disabled-alias").WithName("ScopeExplicitDisabledAlias");
        Assert.Equal(new[] { "enable:" + SamplePolicies.Browser }, CorsKinds(Route(app, "/scope/explicit-disabled-alias")));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await Send(client, "/scope/explicit-disabled-alias", SamplePolicies.BrowserOrigin);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SamplePolicies.BrowserOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Endpoint_metadata_does_not_install_native_CORS_middleware_even_for_no_Origin_requests()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false, useCors: false);
            if (native)
            {
                app.MapGet("/scope/setter", () => TypedResults.Ok(new ScopeReceipt("setter"))).RequireCors(SamplePolicies.Browser);
            }
            else
            {
                app.MapMediumApiEndpoint<SetterEndpoint>();
            }

            await app.StartAsync();
            using var client = app.GetTestClient();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                using var response = await Send(client, "/scope/setter");
            });
            Assert.Contains("CORS", error.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Endpoint_metadata_does_not_register_native_CORS_services()
    {
        var errors = new List<InvalidOperationException>();
        foreach (var native in new[] { true, false })
        {
            await using var app = BareHost();
            app.UseRouting();
            app.UseCors();
            if (native)
            {
                app.MapGet("/scope/setter", () => TypedResults.Ok(new ScopeReceipt("setter"))).RequireCors(SamplePolicies.Browser);
            }
            else
            {
                app.MapMediumApiEndpoint<SetterEndpoint>();
            }

            Assert.Null(app.Services.GetService<ICorsService>());
            Assert.Null(app.Services.GetService<ICorsPolicyProvider>());
            Assert.NotNull(Route(app, "/scope/setter").Metadata.GetMetadata<ICorsMetadata>());
            errors.Add(await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync()));
        }

        Assert.Equal(2, errors.Count);
        Assert.False(string.IsNullOrWhiteSpace(errors[0].Message));
        Assert.Equal(errors[0].GetType(), errors[1].GetType());
        Assert.Equal(errors[0].Message, errors[1].Message);
    }

    [Fact]
    public async Task An_unregistered_named_policy_preserves_native_handler_execution_for_normal_and_preflight_requests()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            if (native)
            {
                app.MapGet("/scope/missing-policy", () => TypedResults.Ok(new ScopeReceipt("missing-policy"))).RequireCors("NotRegistered");
            }
            else
            {
                app.MapMediumApiEndpoint<MissingPolicyEndpoint>();
            }

            await app.StartAsync();
            using var client = app.GetTestClient();
            using var normal = await Send(client, "/scope/missing-policy", SamplePolicies.BrowserOrigin);
            Assert.Equal(HttpStatusCode.OK, normal.StatusCode);
            Assert.Equal(new ScopeReceipt("missing-policy"), await normal.Content.ReadFromJsonAsync<ScopeReceipt>());
            Assert.False(normal.Headers.Contains("Access-Control-Allow-Origin"));
            using var preflight = await Preflight(client, "/scope/missing-policy", SamplePolicies.BrowserOrigin);
            Assert.Equal(HttpStatusCode.OK, preflight.StatusCode);
            Assert.Equal(new ScopeReceipt("missing-policy"), await preflight.Content.ReadFromJsonAsync<ScopeReceipt>());
            Assert.False(preflight.Headers.Contains("Access-Control-Allow-Origin"));
        }
    }

    [Fact]
    public async Task Class_CORS_metadata_preserves_native_class_authorization_challenge_and_success()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = BareHost(withAuthentication: true);
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            if (native)
            {
                app.MapGroup("").WithMetadata(new AuthorizeAttribute(), new EnableCorsAttribute(SamplePolicies.Browser)).MapGet("/scope/mixed", () => TypedResults.Ok(new ScopeReceipt("mixed")));
            }
            else
            {
                app.MapMediumApiEndpoint<MixedAuthorizationEndpoint>();
            }

            app.MapPost("/_scope/sign-in", async (HttpContext context) =>
            {
                await context.SignInAsync("ScopeCookie", new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "synthetic-reader")], "ScopeCookie")));
                return TypedResults.NoContent();
            }).AllowAnonymous();
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var rejected = await Send(client, "/scope/mixed", SamplePolicies.BrowserOrigin);
            Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
            Assert.Equal(SamplePolicies.BrowserOrigin, Assert.Single(rejected.Headers.GetValues("Access-Control-Allow-Origin")));
            using var signIn = await client.PostAsync("/_scope/sign-in", content: null);
            Assert.Equal(HttpStatusCode.NoContent, signIn.StatusCode);
            var cookie = Assert.Single(signIn.Headers.GetValues("Set-Cookie"), value => value.StartsWith("MediumApi.Scope.Cookie=", StringComparison.Ordinal)).Split(';', 2)[0];
            using var request = new HttpRequestMessage(HttpMethod.Get, "/scope/mixed");
            request.Headers.Add("Origin", SamplePolicies.BrowserOrigin);
            request.Headers.Add("Cookie", cookie);
            using var accepted = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            Assert.Equal(new ScopeReceipt("mixed"), await accepted.Content.ReadFromJsonAsync<ScopeReceipt>());
            Assert.Equal(SamplePolicies.BrowserOrigin, Assert.Single(accepted.Headers.GetValues("Access-Control-Allow-Origin")));
        }
    }

    private static string[] CorsKinds(RouteEndpoint endpoint) => endpoint.Metadata.GetOrderedMetadata<ICorsMetadata>().Select(value => value is IDisableCorsAttribute ? "disable" : "enable:" + ((IEnableCorsAttribute)value).PolicyName).ToArray();

    private static WebApplication BareHost(bool withAuthentication = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(EndpointCorsScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        if (withAuthentication)
        {
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
            builder.Services.AddCors(options => options.AddPolicy(SamplePolicies.Browser, policy => policy.WithOrigins(SamplePolicies.BrowserOrigin).AllowAnyHeader().AllowAnyMethod()));
            builder.Services.AddAuthentication("ScopeCookie").AddCookie("ScopeCookie", options =>
            {
                options.Cookie.Name = "MediumApi.Scope.Cookie";
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            });
            builder.Services.AddAuthorization();
        }

        return builder.Build();
    }

    [EnableCors(SamplePolicies.Browser)]
    private static ScopeMetadataResult NativeOrdered() => new();
}