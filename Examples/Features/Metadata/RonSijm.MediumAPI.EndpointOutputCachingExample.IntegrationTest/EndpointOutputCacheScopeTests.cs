using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Disabled;
using RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Disabled.Child;
using RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Inheritance;
using RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Manual;
using RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Ordered;
using RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Scoped;
using RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Selected;
using Xunit;
using static RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.OutputCacheHost;

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest;

public sealed class EndpointOutputCacheScopeTests
{
    [Fact]
    public async Task Class_cache_metadata_precedes_inferred_provider_method_and_Configure_metadata()
    {
        await using var native = ScopeHost();
        await using var generated = ScopeHost();
        var group = native.MapGroup("/scope/ordered").WithMetadata(new OutputCacheAttribute { Duration = 120, Tags = ["group"] });
        group.WithMetadata(new OutputCacheAttribute { Duration = 240, Tags = ["class"], VaryByRouteValueNames = ["tenant"] });
        group.MapGet("/value", NativeOrdered).WithMetadata(new OutputCacheAttribute { Duration = 600, Tags = ["configure"] });
        generated.MapMediumApiNamespace<OrderedGroupDefinition>().MapMediumApiEndpoint<OrderedEndpoint>();
        var expected = new[] { 120, 240, 360, 480, 600 };
        Assert.Equal(expected, Route(native, "/scope/ordered/value").Metadata.GetOrderedMetadata<OutputCacheAttribute>().Select(attribute => attribute.Duration));
        var metadata = Route(generated, "/scope/ordered/value").Metadata;
        var attributes = metadata.GetOrderedMetadata<OutputCacheAttribute>();
        Assert.Equal(expected, attributes.Select(attribute => attribute.Duration));
        Assert.Equal(new[] { "group", "class", "provider", "method", "configure" }, attributes.Select(attribute => Assert.Single(attribute.Tags!)));
        Assert.Equal(new[] { "tenant" }, attributes[1].VaryByRouteValueNames);
        Assert.Equal(typeof(OrderedEndpoint), metadata.GetMetadata<MethodInfo>()!.DeclaringType);
    }

    [Fact]
    public async Task Base_class_cache_attributes_are_ignored_while_original_inherited_handler_metadata_remains()
    {
        await using var app = ScopeHost();
        app.MapMediumApiEndpoint<BaseClassIgnoredEndpoint>();
        app.MapMediumApiEndpoint<InheritedMethodEndpoint>();
        Assert.Null(Route(app, "/scope/base-class-ignored").Metadata.GetMetadata<OutputCacheAttribute>());
        var inherited = Route(app, "/scope/inherited-method").Metadata;
        var attributes = inherited.GetOrderedMetadata<OutputCacheAttribute>();
        Assert.Equal(2, attributes.Count);
        Assert.Equal(600, attributes[0].Duration);
        Assert.True(attributes[1].NoStore);
        Assert.Same(attributes[1], inherited.GetMetadata<OutputCacheAttribute>());
        Assert.Equal(typeof(BaseMethodEndpoint), inherited.GetMetadata<MethodInfo>()!.DeclaringType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var (path, kind) in new[] { ("/scope/base-class-ignored", "base-class-ignored"), ("/scope/inherited-method", "inherited-method") })
        {
            Assert.Equal(new ScopeReceipt(kind, 1), await ReadReceipt(client, path));
            Assert.Equal(new ScopeReceipt(kind, 2), await ReadReceipt(client, path));
        }
    }

    [Fact]
    public async Task Unrelated_cache_policy_and_MVC_attributes_are_not_constructed_or_executed()
    {
        await using var app = ScopeHost();
        app.MapMediumApiEndpoint<IgnoredAttributesEndpoint>();
        var metadata = Route(app, "/scope/ignored-attributes").Metadata;
        Assert.Single(metadata.GetOrderedMetadata<OutputCacheAttribute>());
        Assert.Null(metadata.GetMetadata<IOutputCachePolicy>());
        Assert.DoesNotContain(metadata, value => value is UnrelatedCachePolicyAttribute or UnrelatedMvcAttribute);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new ScopeReceipt("ignored-attributes", 1), await ReadReceipt(client, "/scope/ignored-attributes"));
        Assert.Equal(new ScopeReceipt("ignored-attributes", 1), await ReadReceipt(client, "/scope/ignored-attributes"));
    }

    [Fact]
    public async Task Repeated_mounts_and_independent_hosts_receive_fresh_attribute_objects_and_named_arrays()
    {
        await using var repeated = ScopeHost();
        await using var independent = ScopeHost();
        repeated.MapMediumApiEndpoint<FreshAttributesEndpoint>("/scope/first").WithName("ScopeFirst");
        repeated.MapMediumApiEndpoint<FreshAttributesEndpoint>("/scope/second").WithName("ScopeSecond");
        independent.MapMediumApiEndpoint<FreshAttributesEndpoint>();
        var first = Route(repeated, "/scope/first").Metadata.GetMetadata<OutputCacheAttribute>()!;
        var second = Route(repeated, "/scope/second").Metadata.GetMetadata<OutputCacheAttribute>()!;
        var otherHost = Route(independent, "/scope/fresh").Metadata.GetMetadata<OutputCacheAttribute>()!;
        Assert.NotSame(first, second);
        Assert.NotSame(first, otherHost);
        foreach (var other in new[] { second, otherHost })
        {
            Assert.NotSame(first.Tags, other.Tags);
            Assert.NotSame(first.VaryByQueryKeys, other.VaryByQueryKeys);
            Assert.NotSame(first.VaryByHeaderNames, other.VaryByHeaderNames);
            Assert.NotSame(first.VaryByRouteValueNames, other.VaryByRouteValueNames);
        }

        first.Tags![0] = "changed";
        first.VaryByQueryKeys![0] = "changed";
        first.VaryByHeaderNames![0] = "changed";
        first.VaryByRouteValueNames![0] = "changed";
        foreach (var other in new[] { second, otherHost })
        {
            Assert.Equal(new[] { "scope" }, other.Tags);
            Assert.Equal(new[] { "category" }, other.VaryByQueryKeys);
            Assert.Equal(new[] { "X-Scope-Language" }, other.VaryByHeaderNames);
            Assert.Equal(new[] { "tenant" }, other.VaryByRouteValueNames);
        }
    }

    [Fact]
    public async Task An_unset_duration_and_explicit_zero_keep_their_distinct_native_cache_behavior()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = ScopeHost();
            if (native)
            {
                app.MapGet("/scope/default", (ScopeProbe probe) => TypedResults.Ok(probe.Record("default"))).WithMetadata(new OutputCacheAttribute());
                app.MapGet("/scope/zero", (ScopeProbe probe) => TypedResults.Ok(probe.Record("zero"))).WithMetadata(new OutputCacheAttribute { Duration = 0 });
            }
            else
            {
                app.MapMediumApiEndpoint<DefaultDurationEndpoint>();
                app.MapMediumApiEndpoint<ZeroDurationEndpoint>();
            }

            Assert.Equal(0, Route(app, "/scope/default").Metadata.GetMetadata<OutputCacheAttribute>()!.Duration);
            Assert.Equal(0, Route(app, "/scope/zero").Metadata.GetMetadata<OutputCacheAttribute>()!.Duration);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new ScopeReceipt("default", 1), await ReadReceipt(client, "/scope/default"));
            Assert.Equal(new ScopeReceipt("default", 1), await ReadReceipt(client, "/scope/default"));
            Assert.Equal(new ScopeReceipt("zero", 1), await ReadReceipt(client, "/scope/zero"));
            Assert.Equal(new ScopeReceipt("zero", 2), await ReadReceipt(client, "/scope/zero"));
        }
    }

    [Fact]
    public async Task Native_cache_hits_skip_endpoint_activation_and_owned_disposal_after_the_first_dispatch()
    {
        await using var app = ScopeHost();
        app.MapMediumApiEndpoint<ActivationEndpoint>();
        Assert.NotNull(Route(app, "/scope/activation").Metadata.GetMetadata<OutputCacheAttribute>());
        var probe = app.Services.GetRequiredService<ScopeProbe>();
        Assert.Equal(0, probe.Constructions);
        Assert.Equal(0, probe.Disposals);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new ScopeReceipt("activation", 1), await ReadReceipt(client, "/scope/activation"));
        Assert.Equal(new ScopeReceipt("activation", 1), await ReadReceipt(client, "/scope/activation"));
        Assert.Equal(1, probe.Constructions);
        Assert.Equal(1, probe.Disposals);
        Assert.Equal(1, probe.CallsFor("activation"));
    }

    [Fact]
    public async Task Declaration_manifests_and_disabled_or_manual_automatic_selection_do_not_materialize_class_policies()
    {
        await using var app = ScopeHost();
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
        Assert.Equal(0, app.Services.GetRequiredService<ScopeProbe>().Constructions);
    }

    [Fact]
    public async Task Disabled_typed_mounts_skip_descendant_conditions_and_callbacks()
    {
        await using var app = ScopeHost();
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callbackInvoked = true));
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        Assert.Empty(Routes(app));
    }

    [Fact]
    public async Task Explicit_aliases_keep_class_caching_without_implicitly_importing_namespace_metadata_or_conditions()
    {
        await using var app = ScopeHost();
        app.MapMediumApiNamespace<ScopedGroupDefinition>().MapMediumApiEndpoint<ScopedEndpoint>();
        app.MapMediumApiEndpoint<ScopedEndpoint>("/scope/alias").WithName("ScopeAlias");
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        app.MapMediumApiEndpoint<DisabledAliasEndpoint>("/scope/explicit-disabled-alias").WithName("ScopeDisabledAlias");
        Assert.Equal(2, Route(app, "/scope/scoped/value").Metadata.GetOrderedMetadata<OutputCacheAttribute>().Count);
        Assert.Single(Route(app, "/scope/alias").Metadata.GetOrderedMetadata<OutputCacheAttribute>());
        Assert.Single(Route(app, "/scope/explicit-disabled-alias").Metadata.GetOrderedMetadata<OutputCacheAttribute>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new ScopeReceipt("scoped", 1), await ReadReceipt(client, "/scope/scoped/value"));
        Assert.Equal(new ScopeReceipt("scoped", 2), await ReadReceipt(client, "/scope/alias"));
        Assert.Equal(new ScopeReceipt("scoped", 1), await ReadReceipt(client, "/scope/scoped/value"));
        Assert.Equal(new ScopeReceipt("scoped", 2), await ReadReceipt(client, "/scope/alias"));
        Assert.Equal(new ScopeReceipt("disabled-alias", 1), await ReadReceipt(client, "/scope/explicit-disabled-alias"));
        Assert.Equal(new ScopeReceipt("disabled-alias", 1), await ReadReceipt(client, "/scope/explicit-disabled-alias"));
    }

    [Fact]
    public async Task Metadata_without_services_or_middleware_is_only_metadata_and_keeps_handlers_uncached()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = BareHost();
            app.UseRouting();
            MapDefault(app, native);
            Assert.Null(app.Services.GetService<IOutputCacheStore>());
            Assert.NotNull(Route(app, "/scope/default").Metadata.GetMetadata<OutputCacheAttribute>());
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new ScopeReceipt("default", 1), await ReadReceipt(client, "/scope/default"));
            Assert.Equal(new ScopeReceipt("default", 2), await ReadReceipt(client, "/scope/default"));
        }
    }

    [Fact]
    public async Task Registered_cache_services_still_require_native_middleware_to_cache_responses()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = ScopeHost(useOutputCache: false);
            MapDefault(app, native);
            Assert.NotNull(app.Services.GetRequiredService<IOutputCacheStore>());
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new ScopeReceipt("default", 1), await ReadReceipt(client, "/scope/default"));
            Assert.Equal(new ScopeReceipt("default", 2), await ReadReceipt(client, "/scope/default"));
        }
    }

    [Fact]
    public async Task Missing_cache_services_preserve_the_actual_native_middleware_startup_failure()
    {
        var errors = new List<InvalidOperationException>();
        foreach (var native in new[] { true, false })
        {
            await using var app = BareHost();
            app.UseRouting();
            app.UseOutputCache();
            MapDefault(app, native);
            Assert.Null(app.Services.GetService<IOutputCacheStore>());
            Assert.NotNull(Route(app, "/scope/default").Metadata.GetMetadata<OutputCacheAttribute>());
            errors.Add(await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync()));
        }

        Assert.False(string.IsNullOrWhiteSpace(errors[0].Message));
        Assert.Equal(errors[0].GetType(), errors[1].GetType());
        Assert.Equal(errors[0].Message, errors[1].Message);
    }

    [Fact]
    public async Task A_missing_named_policy_is_a_native_no_op_and_does_not_implicitly_add_the_default_policy()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = ScopeHost();
            if (native)
            {
                app.MapGet("/scope/missing-policy", (ScopeProbe probe) => TypedResults.Ok(probe.Record("missing-policy"))).WithMetadata(new OutputCacheAttribute { PolicyName = "NotRegistered" });
            }
            else
            {
                app.MapMediumApiEndpoint<MissingPolicyEndpoint>();
            }

            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new ScopeReceipt("missing-policy", 1), await ReadReceipt(client, "/scope/missing-policy"));
            Assert.Equal(new ScopeReceipt("missing-policy", 2), await ReadReceipt(client, "/scope/missing-policy"));
        }
    }

    [Fact]
    public async Task Class_authorization_with_native_cookie_identity_preserves_default_authenticated_request_cache_exclusion()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = BareHost(withAuthentication: true);
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseOutputCache();
            if (native)
            {
                app.MapGroup("").WithMetadata(new AuthorizeAttribute(), new OutputCacheAttribute()).MapGet("/scope/mixed", (ScopeProbe probe) => TypedResults.Ok(probe.Record("mixed")));
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
            using var rejected = await client.GetAsync("/scope/mixed");
            Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
            Assert.Equal(0, app.Services.GetRequiredService<ScopeProbe>().CallsFor("mixed"));
            using var signIn = await client.PostAsync("/_scope/sign-in", content: null);
            Assert.Equal(HttpStatusCode.NoContent, signIn.StatusCode);
            var cookie = Assert.Single(signIn.Headers.GetValues("Set-Cookie"), value => value.StartsWith("MediumApi.Scope.OutputCache=", StringComparison.Ordinal)).Split(';', 2)[0];
            Assert.Equal(new ScopeReceipt("mixed", 1), await ReadReceipt(client, "/scope/mixed", cookie));
            Assert.Equal(new ScopeReceipt("mixed", 2), await ReadReceipt(client, "/scope/mixed", cookie));
        }
    }

    private static WebApplication ScopeHost(bool useOutputCache = true) => Create(mapEndpoints: false, useOutputCache: useOutputCache, configure: builder => builder.Services.AddSingleton<ScopeProbe>());

    private static async Task<ScopeReceipt> ReadReceipt(HttpClient client, string path, string? cookie = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", cookie);
        }

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ScopeReceipt>())!;
    }

    private static void MapDefault(WebApplication app, bool native)
    {
        if (native)
        {
            app.MapGet("/scope/default", (ScopeProbe probe) => TypedResults.Ok(probe.Record("default"))).WithMetadata(new OutputCacheAttribute());
        }
        else
        {
            app.MapMediumApiEndpoint<DefaultDurationEndpoint>();
        }
    }

    private static WebApplication BareHost(bool withAuthentication = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(EndpointOutputCacheScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ScopeProbe>();
        if (withAuthentication)
        {
            builder.Services.AddOutputCache();
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
            builder.Services.AddAuthentication("ScopeCookie").AddCookie("ScopeCookie", options =>
            {
                options.Cookie.Name = "MediumApi.Scope.OutputCache";
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

    [OutputCache(Duration = 480, Tags = new[] { "method" })]
    private static ScopeMetadataResult NativeOrdered() => new();
}