using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Disabled;
using RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Disabled.Child;
using RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Inheritance;
using RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Manual;
using RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Ordered;
using RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Scoped;
using RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Selected;
using Xunit;
using static RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.EndpointRateLimitHost;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest;

public sealed class EndpointRateLimitScopeTests
{
    [Fact]
    public async Task Class_rate_metadata_precedes_inferred_provider_original_method_and_Configure_metadata()
    {
        await using var native = Create(mapEndpoints: false);
        await using var generated = Create(mapEndpoints: false);
        native.MapGroup("/scope/ordered").RequireRateLimiting(RateLimitPolicies.Group).WithMetadata(new EnableRateLimitingAttribute(RateLimitPolicies.Class)).MapGet("/value", NativeOrdered).RequireRateLimiting(RateLimitPolicies.Fluent);
        generated.MapMediumApiNamespace<OrderedGroupDefinition>().MapMediumApiEndpoint<OrderedEndpoint>();
        var expected = new[] { RateLimitPolicies.Group, RateLimitPolicies.Class, "scope-provider", RateLimitPolicies.Method, RateLimitPolicies.Fluent };
        Assert.Equal(expected, Route(native, "/scope/ordered/value").Metadata.GetOrderedMetadata<EnableRateLimitingAttribute>().Select(attribute => attribute.PolicyName));
        var metadata = Route(generated, "/scope/ordered/value").Metadata;
        var attributes = metadata.GetOrderedMetadata<EnableRateLimitingAttribute>();
        Assert.Equal(expected, attributes.Select(attribute => attribute.PolicyName));
        Assert.Same(attributes[^1], metadata.GetMetadata<EnableRateLimitingAttribute>());
        Assert.Equal(typeof(OrderedEndpoint), metadata.GetMetadata<MethodInfo>()!.DeclaringType);
    }

    [Fact]
    public async Task CLR_base_class_attributes_are_ignored_but_original_inherited_handler_attributes_remain()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<BaseClassIgnoredEndpoint>();
        app.MapMediumApiEndpoint<InheritedMethodEndpoint>();
        var plain = Route(app, "/scope/base-class-ignored").Metadata;
        Assert.Empty(plain.GetOrderedMetadata<EnableRateLimitingAttribute>());
        Assert.Null(plain.GetMetadata<DisableRateLimitingAttribute>());
        var inherited = Route(app, "/scope/inherited-method").Metadata;
        Assert.Equal(new[] { RateLimitPolicies.Class, RateLimitPolicies.Method }, inherited.GetOrderedMetadata<EnableRateLimitingAttribute>().Select(attribute => attribute.PolicyName));
        Assert.Null(inherited.GetMetadata<DisableRateLimitingAttribute>());
        Assert.Equal(typeof(BaseMethodEndpoint), inherited.GetMetadata<MethodInfo>()!.DeclaringType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        await AssertAllowed(client, "/scope/base-class-ignored", "base-class-ignored");
        await AssertAllowed(client, "/scope/base-class-ignored", "base-class-ignored");
        await AssertAllowed(client, "/scope/inherited-method", "inherited-method");
    }

    [Fact]
    public async Task Only_the_exact_sealed_native_attributes_are_selected_and_unrelated_policy_attributes_remain_unconstructed()
    {
        Assert.True(typeof(EnableRateLimitingAttribute).IsSealed);
        Assert.True(typeof(DisableRateLimitingAttribute).IsSealed);
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<IgnoredAttributesEndpoint>();
        var metadata = Route(app, "/scope/ignored-attributes").Metadata;
        Assert.Single(metadata.GetOrderedMetadata<EnableRateLimitingAttribute>());
        Assert.Null(metadata.GetMetadata<IRateLimiterPolicy<string>>());
        Assert.DoesNotContain(metadata, value => value is UnrelatedRatePolicyAttribute or UnrelatedMvcAttribute);
        await app.StartAsync();
        using var client = app.GetTestClient();
        await AssertAllowed(client, "/scope/ignored-attributes", "ignored-attributes");
        await AssertRejected(client, "/scope/ignored-attributes");
    }

    [Fact]
    public async Task Repeated_mounts_and_independent_hosts_receive_fresh_enable_and_disable_attribute_objects()
    {
        await using var repeated = Create(mapEndpoints: false);
        await using var independent = Create(mapEndpoints: false);
        repeated.MapMediumApiEndpoint<FreshEnableEndpoint>("/scope/first-enable").WithName("FirstEnable");
        repeated.MapMediumApiEndpoint<FreshEnableEndpoint>("/scope/second-enable").WithName("SecondEnable");
        repeated.MapMediumApiEndpoint<FreshDisableEndpoint>("/scope/first-disable").WithName("FirstDisable");
        repeated.MapMediumApiEndpoint<FreshDisableEndpoint>("/scope/second-disable").WithName("SecondDisable");
        independent.MapMediumApiEndpoint<FreshEnableEndpoint>();
        independent.MapMediumApiEndpoint<FreshDisableEndpoint>();
        var firstEnable = Route(repeated, "/scope/first-enable").Metadata.GetMetadata<EnableRateLimitingAttribute>();
        var secondEnable = Route(repeated, "/scope/second-enable").Metadata.GetMetadata<EnableRateLimitingAttribute>();
        var otherEnable = Route(independent, "/scope/fresh-enable").Metadata.GetMetadata<EnableRateLimitingAttribute>();
        Assert.Equal(RateLimitPolicies.Class, firstEnable!.PolicyName);
        Assert.Equal(firstEnable.PolicyName, secondEnable!.PolicyName);
        Assert.Equal(firstEnable.PolicyName, otherEnable!.PolicyName);
        Assert.NotSame(firstEnable, secondEnable);
        Assert.NotSame(firstEnable, otherEnable);
        var firstDisable = Route(repeated, "/scope/first-disable").Metadata.GetMetadata<DisableRateLimitingAttribute>();
        var secondDisable = Route(repeated, "/scope/second-disable").Metadata.GetMetadata<DisableRateLimitingAttribute>();
        var otherDisable = Route(independent, "/scope/fresh-disable").Metadata.GetMetadata<DisableRateLimitingAttribute>();
        Assert.NotNull(firstDisable);
        Assert.NotNull(secondDisable);
        Assert.NotNull(otherDisable);
        Assert.NotSame(firstDisable, secondDisable);
        Assert.NotSame(firstDisable, otherDisable);
    }

    [Fact]
    public async Task A_native_rate_rejection_occurs_before_endpoint_target_activation()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapGet("/scope/consume", (RateLimitProbe probe) => Reply(probe, "consume")).RequireRateLimiting(RateLimitPolicies.Class);
        app.MapMediumApiEndpoint<ActivationEndpoint>();
        Assert.NotNull(Route(app, "/scope/activation").Metadata.GetMetadata<EnableRateLimitingAttribute>());
        Assert.Equal(0, Probe(app).CallsFor("activation-construction"));
        await app.StartAsync();
        using var client = app.GetTestClient();
        await AssertAllowed(client, "/scope/consume", "consume");
        await AssertRejected(client, "/scope/activation");
        Assert.Equal(0, Probe(app).CallsFor("activation-construction"));
        Assert.Equal(0, Probe(app).CallsFor("activation"));
        Assert.Equal(1, Probe(app).TotalCalls);
    }

    [Fact]
    public async Task Declaration_manifests_and_disabled_or_manual_automatic_selection_do_not_materialize_class_metadata()
    {
        await using var app = Create(mapEndpoints: false);
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(ManualThrowingEndpoint) && endpoint.IsManuallyMapped);
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(DisabledEndpoint));
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(DisabledChildEndpoint));
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Empty(Routes(app));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/disabled");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, Probe(app).TotalCalls);
    }

    [Fact]
    public async Task Disabled_typed_mounts_skip_descendant_conditions_mapping_callbacks_and_class_metadata()
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
    public async Task Explicit_aliases_keep_class_rate_metadata_without_importing_namespace_markers_or_conditions()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ScopedGroupDefinition>().MapMediumApiEndpoint<ScopedEndpoint>();
        app.MapMediumApiEndpoint<ScopedEndpoint>("/scope/alias").WithName("ScopedAlias");
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        app.MapMediumApiEndpoint<DisabledAliasEndpoint>("/scope/explicit-disabled-alias").WithName("ExplicitDisabledAlias");
        Assert.NotNull(Route(app, "/scope/scoped/value").Metadata.GetMetadata<DisableRateLimitingAttribute>());
        Assert.Null(Route(app, "/scope/alias").Metadata.GetMetadata<DisableRateLimitingAttribute>());
        Assert.Null(Route(app, "/scope/explicit-disabled-alias").Metadata.GetMetadata<DisableRateLimitingAttribute>());
        Assert.Equal(RateLimitPolicies.Class, Route(app, "/scope/explicit-disabled-alias").Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName);
        await app.StartAsync();
        using var client = app.GetTestClient();
        await AssertAllowed(client, "/scope/scoped/value", "scoped");
        await AssertAllowed(client, "/scope/scoped/value", "scoped");
        await AssertAllowed(client, "/scope/alias", "scoped");
        await AssertRejected(client, "/scope/explicit-disabled-alias");
    }

    [Fact]
    public async Task Metadata_without_services_or_middleware_remains_inert_like_native_metadata()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = BareHost();
            app.UseRouting();
            MapMissing(app, native);
            Assert.Equal(RateLimitPolicies.Missing, Route(app, "/scope/missing-policy").Metadata.GetMetadata<EnableRateLimitingAttribute>()!.PolicyName);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await AssertAllowed(client, "/scope/missing-policy", "missing-policy");
            await AssertAllowed(client, "/scope/missing-policy", "missing-policy");
            Assert.Equal(2, Probe(app).TotalCalls);
        }
    }

    [Fact]
    public async Task Registered_native_services_still_need_native_middleware_to_limit_requests()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false, useRateLimiter: false);
            MapMissing(app, native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            await AssertAllowed(client, "/scope/missing-policy", "missing-policy");
            await AssertAllowed(client, "/scope/missing-policy", "missing-policy");
            Assert.Equal(0, Probe(app).Rejections);
        }
    }

    [Fact]
    public async Task Missing_services_preserve_the_native_UseRateLimiter_registration_failure()
    {
        var errors = new List<InvalidOperationException>();
        foreach (var native in new[] { true, false })
        {
            await using var app = BareHost();
            app.UseRouting();
            MapMissing(app, native);
            Assert.NotNull(Route(app, "/scope/missing-policy").Metadata.GetMetadata<EnableRateLimitingAttribute>());
            errors.Add(Assert.Throws<InvalidOperationException>(() => { app.UseRateLimiter(); }));
        }

        Assert.False(string.IsNullOrWhiteSpace(errors[0].Message));
        Assert.Equal(errors[0].GetType(), errors[1].GetType());
        Assert.Equal(errors[0].Message, errors[1].Message);
    }

    [Fact]
    public async Task Missing_named_policies_fail_at_request_time_with_the_original_native_error()
    {
        var errors = new List<InvalidOperationException>();
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            MapMissing(app, native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            errors.Add(await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/scope/missing-policy")));
            Assert.Equal(0, Probe(app).TotalCalls);
            Assert.Equal(0, Probe(app).Rejections);
        }

        Assert.Contains(RateLimitPolicies.Missing, errors[0].Message);
        Assert.Equal(errors[0].GetType(), errors[1].GetType());
        Assert.Equal(errors[0].Message, errors[1].Message);
    }

    [Fact]
    public async Task A_class_disable_marker_skips_global_and_missing_method_policy_resolution_like_native_metadata()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false, global: true);
            if (native)
            {
                app.MapGroup("").DisableRateLimiting().MapGet("/scope/disabled-missing-policy", (RateLimitProbe probe) => Reply(probe, "disabled-missing-policy")).RequireRateLimiting(RateLimitPolicies.Missing);
            }
            else
            {
                app.MapMediumApiEndpoint<DisabledMissingPolicyEndpoint>();
            }

            await app.StartAsync();
            using var client = app.GetTestClient();
            for (var request = 0; request < 3; request++)
            {
                await AssertAllowed(client, "/scope/disabled-missing-policy", "disabled-missing-policy");
            }

            Assert.Equal(3, Probe(app).TotalCalls);
            Assert.Equal(0, Probe(app).Rejections);
        }
    }

    [Fact]
    public async Task Native_cookie_authorization_before_rate_limiting_keeps_unauthorized_requests_out_of_the_quota()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = BareHost(withAuthentication: true);
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();
            if (native)
            {
                app.MapGroup("").WithMetadata(new AuthorizeAttribute(), new EnableRateLimitingAttribute(RateLimitPolicies.Class)).MapGet("/scope/mixed", (RateLimitProbe probe) => Reply(probe, "mixed"));
            }
            else
            {
                app.MapMediumApiEndpoint<MixedAuthorizationEndpoint>();
            }

            app.MapPost("/_scope/sign-in", async (HttpContext context) =>
            {
                await context.SignInAsync("ScopeCookie", new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "synthetic-reader")], "ScopeCookie")));
                return TypedResults.NoContent();
            }).AllowAnonymous().DisableRateLimiting();
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var unauthorized = await client.GetAsync("/scope/mixed");
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
            Assert.Equal(0, Probe(app).CallsFor("mixed"));
            using var signIn = await client.PostAsync("/_scope/sign-in", content: null);
            Assert.Equal(HttpStatusCode.NoContent, signIn.StatusCode);
            var cookie = Assert.Single(signIn.Headers.GetValues("Set-Cookie"), value => value.StartsWith("MediumApi.Scope.RateLimit=", StringComparison.Ordinal)).Split(';', 2)[0];
            foreach (var expected in new[] { HttpStatusCode.OK, HttpStatusCode.TooManyRequests })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/scope/mixed");
                request.Headers.Add("Cookie", cookie);
                using var response = await client.SendAsync(request);
                Assert.Equal(expected, response.StatusCode);
            }

            Assert.Equal(1, Probe(app).CallsFor("mixed"));
        }
    }

    private static void MapMissing(WebApplication app, bool native)
    {
        if (native)
        {
            app.MapGet("/scope/missing-policy", (RateLimitProbe probe) => Reply(probe, "missing-policy")).WithMetadata(new EnableRateLimitingAttribute(RateLimitPolicies.Missing));
        }
        else
        {
            app.MapMediumApiEndpoint<MissingPolicyEndpoint>();
        }
    }

    private static WebApplication BareHost(bool withAuthentication = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(EndpointRateLimitScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<RateLimitProbe>();
        if (withAuthentication)
        {
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddFixedWindowLimiter(RateLimitPolicies.Class, limiter =>
                {
                    limiter.PermitLimit = 1;
                    limiter.QueueLimit = 0;
                    limiter.Window = TimeSpan.FromDays(1);
                    limiter.AutoReplenishment = false;
                });
            });
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
            builder.Services.AddAuthentication("ScopeCookie").AddCookie("ScopeCookie", options =>
            {
                options.Cookie.Name = "MediumApi.Scope.RateLimit";
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

    [EnableRateLimiting(RateLimitPolicies.Method)]
    private static ScopeMetadataResult NativeOrdered() => new();
}