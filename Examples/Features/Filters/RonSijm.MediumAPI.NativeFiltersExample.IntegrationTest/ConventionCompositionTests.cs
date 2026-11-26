using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.NativeFiltersExample.AccessPolicyEndpoints;
using RonSijm.MediumAPI.NativeFiltersExample.AccessPolicyEndpoints.Disabled;
using Xunit;

namespace RonSijm.MediumAPI.NativeFiltersExample.IntegrationTest;

public sealed class ConventionCompositionTests
{
    [Theory]
    [InlineData(false, 401)]
    [InlineData(true, 403)]
    public async Task Access_only_authentication_and_authorization_reject_before_owner_headers(bool signedIn, int status)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var cookie = signedIn ? await SignIn(client, member: false) : null;
            using var response = await Send(client, HttpMethod.Get, "/access/read/7", cookie, BrowserPolicies.Origin);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal(BrowserPolicies.Origin, Header(response, "Access-Control-Allow-Origin"));
            Assert.False(response.Headers.CacheControl?.NoStore == true);
            AssertCounts(app, 0, 0, 0);
        }
    }

    [Theory]
    [InlineData("GET", "/access/read/7")]
    [InlineData("POST", "/access/value/7")]
    public async Task Access_only_requests_keep_their_authorization_CORS_and_header_contract_without_a_token_guard(string method, string path)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, new HttpMethod(method), path, await SignIn(client), BrowserPolicies.Origin);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(7, (await response.Content.ReadFromJsonAsync<BrowserReceipt>())!.Value);
            Assert.Equal(BrowserPolicies.Origin, Header(response, "Access-Control-Allow-Origin"));
            Assert.True(response.Headers.CacheControl?.NoStore);
            Assert.Empty(response.Headers.Pragma);
            AssertCounts(app, 0, 0, 1);
        }
    }

    [Fact]
    public async Task Composing_access_policy_does_not_add_or_remove_the_browser_mutation_guard()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native, includeBrowser: true);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var cookie = await SignIn(client);
            using var access = await Send(client, HttpMethod.Post, "/access/value/7", cookie);
            Assert.Equal(HttpStatusCode.OK, access.StatusCode);
            Assert.True(access.Headers.CacheControl?.NoStore);
            using var browser = await Send(client, HttpMethod.Post, "/browser/value/7", cookie);
            Assert.Equal(HttpStatusCode.BadRequest, browser.StatusCode);
            Assert.False(browser.Headers.CacheControl?.NoStore == true);
            AssertCounts(app, 1, 1, 1);
        }
    }

    [Theory]
    [InlineData("reject", 400)]
    [InlineData("failure", 500)]
    public async Task Access_owner_headers_survive_later_results_and_the_existing_failure_boundary(string outcome, int status)
    {
        string? expected = null;
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, HttpMethod.Post, "/access/value/7?outcome=" + outcome, await SignIn(client));
            Assert.Equal(status, (int)response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
            Assert.Empty(response.Headers.Pragma);
            var body = await response.Content.ReadAsStringAsync();
            if (native) { expected = body; }
            else { Assert.Equal(expected, body); }

            AssertCounts(app, 0, 0, 1);
        }
    }

    [Theory]
    [InlineData(BrowserPolicies.Origin, true)]
    [InlineData("https://other.example.test", false)]
    public async Task Native_CORS_preflight_and_actual_access_requests_retain_separate_boundaries(string origin, bool allowed)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var preflight = new HttpRequestMessage(HttpMethod.Options, "/access/value/7");
            preflight.Headers.Add("Origin", origin);
            preflight.Headers.Add("Access-Control-Request-Method", "POST");
            using var options = await client.SendAsync(preflight);
            Assert.Equal(HttpStatusCode.NoContent, options.StatusCode);
            Assert.Equal(allowed, options.Headers.Contains("Access-Control-Allow-Origin"));
            Assert.False(options.Headers.CacheControl?.NoStore == true);
            AssertCounts(app, 0, 0, 0);
            using var actual = await Send(client, HttpMethod.Get, "/access/read/7", await SignIn(client), origin);
            Assert.Equal(HttpStatusCode.OK, actual.StatusCode);
            Assert.Equal(allowed, actual.Headers.Contains("Access-Control-Allow-Origin"));
            Assert.True(actual.Headers.CacheControl?.NoStore);
            AssertCounts(app, 0, 0, 1);
        }
    }

    [Fact]
    public async Task Composed_access_policy_preserves_exact_native_metadata_original_delegates_and_API_Explorer()
    {
        await using var native = Create(native: true);
        await using var app = Create();
        await native.StartAsync();
        await app.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(native.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot(), app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot()));
        foreach (var expected in Routes(native).Where(route => route.RoutePattern.RawText!.StartsWith("/access", StringComparison.Ordinal)))
        {
            var actual = Route(app, expected.RoutePattern.RawText!);
            Assert.Equal(expected.Metadata.GetMetadata<MethodInfo>(), actual.Metadata.GetMetadata<MethodInfo>());
            Assert.Equal(new[] { typeof(AuthorizeAttribute), typeof(EnableCorsAttribute) }, actual.Metadata.Where(value => value is IAuthorizeData or ICorsMetadata).Select(value => value.GetType()));
            Assert.Equal(BrowserPolicies.Workspace, Assert.Single(actual.Metadata.GetOrderedMetadata<IAuthorizeData>()).Policy);
            Assert.Equal(BrowserPolicies.Cors, Assert.Single(actual.Metadata.GetOrderedMetadata<EnableCorsAttribute>()).PolicyName);
        }

        Assert.Null(Route(app, "/access/value-alias/{value:int}").Metadata.GetMetadata<IEndpointNameMetadata>());
    }

    [Fact]
    public async Task Access_mounts_receive_fresh_native_metadata_and_aliases_retain_their_selected_mount()
    {
        await using var app = Create(map: false);
        foreach (var prefix in new[] { "/first", "/second" })
        {
            var group = app.MapGroup(prefix).MapMediumApiNamespace<AccessPolicyGroupDefinition>();
            AccessPolicyScenario.MapEntries(group, suppressNames: true);
        }

        await app.StartAsync();
        var first = Route(app, "/first/access/read/{value:int}").Metadata;
        var second = Route(app, "/second/access/read/{value:int}").Metadata;
        Assert.NotSame(first.GetMetadata<IAuthorizeData>(), second.GetMetadata<IAuthorizeData>());
        Assert.NotSame(first.GetMetadata<EnableCorsAttribute>(), second.GetMetadata<EnableCorsAttribute>());
        Assert.Same(first.GetMetadata<IAuthorizeData>(), Route(app, "/first/access/value-alias/{value:int}").Metadata.GetMetadata<IAuthorizeData>());
        Assert.Null(first.GetMetadata<IEndpointNameMetadata>());
        using var client = app.GetTestClient();
        using var response = await Send(client, HttpMethod.Post, "/second/access/value-alias/7", await SignIn(client));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        AssertCounts(app, 0, 0, 1);
    }

    [Fact]
    public async Task Separate_flags_and_disabled_groups_skip_their_nested_conventions()
    {
        await using var app = Create(enabled: false);
        Assert.Null(app.TryMapMediumApiNamespace<DisabledAccessPolicyGroupDefinition>());
        await app.StartAsync();
        Assert.DoesNotContain(Routes(app), route => route.RoutePattern.RawText!.Contains("/access", StringComparison.Ordinal));
        await using var browser = BrowserPolicyConventionTests.Create();
        await browser.StartAsync();
        Assert.DoesNotContain(Routes(browser), route => route.RoutePattern.RawText!.Contains("/access", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("endpoint")]
    [InlineData("group")]
    public async Task Nested_erased_conventions_dispatch_once_in_order_without_instances_or_implicit_attribute_imports(string scope)
    {
        await using var app = BrowserPolicyConventionTests.Create(map: false);
        IEndpointConventionBuilder builder;
        if (scope == "group")
        {
            var group = app.MapGroup("");
            builder = group;
            Assert.Same(builder, builder.AddMediumApiConvention<OuterConvention>());
            group.MapGet("/composition", CompositionValue);
        }
        else
        {
            builder = app.MapGet("/composition", CompositionValue);
            Assert.Same(builder, builder.AddMediumApiConvention<OuterConvention>());
        }

        await app.StartAsync();
        var metadata = Route(app, "/composition").Metadata;
        Assert.Equal(new[] { "before", "struct", "explicit", "after" }, metadata.GetOrderedMetadata<CompositionStamp>().Select(value => value.Kind));
        Assert.DoesNotContain(metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), value => value.StatusCode == 418);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/composition");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Unused-Composition"));
        Assert.Equal(4, Route(app, "/composition").Metadata.GetOrderedMetadata<CompositionStamp>().Count);
    }

    [Fact]
    public async Task Erased_nested_calls_keep_null_first_and_original_Configure_exception_identity()
    {
        IEndpointConventionBuilder absent = null!;
        Assert.Throws<ArgumentNullException>(() => absent.AddMediumApiConvention<ThrowingConvention>());
        await using var app = BrowserPolicyConventionTests.Create(map: false);
        IEndpointConventionBuilder endpoint = app.MapGet("/throw", CompositionValue);
        Assert.Same(ThrowingConvention.Failure, Assert.Throws<InvalidOperationException>(() => endpoint.AddMediumApiConvention<ThrowingConvention>()));
    }

    private static WebApplication Create(bool native = false, bool enabled = true, bool map = true, bool includeBrowser = false)
    {
        var app = BrowserPolicyConventionTests.Create(map: false);
        app.Configuration["Sample:AccessPolicies"] = enabled.ToString();
        if (map)
        {
            if (native) { MapNative(app, enabled); }
            else { AccessPolicyScenario.Map(app); }
        }

        if (includeBrowser) { BrowserPolicyScenario.Map(app); }

        return app;
    }

    private static async Task<string> SignIn(HttpClient client, bool member = true)
    {
        using var response = await client.PostAsync("/fixture/session/" + (member ? "member" : "visitor"), null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return string.Join("; ", response.Headers.GetValues("Set-Cookie").Select(value => value.Split(';')[0]));
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, string? cookie = null, string? origin = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (cookie is not null) { request.Headers.Add("Cookie", cookie); }

        if (origin is not null) { request.Headers.Add("Origin", origin); }

        return await client.SendAsync(request);
    }

    private static void AssertCounts(WebApplication app, int guards, int antiforgery, int handlers)
    {
        var probe = app.Services.GetRequiredService<BrowserPolicyProbe>();
        Assert.Equal(guards, probe.GuardCalls);
        Assert.Equal(antiforgery, probe.AntiforgeryChecks);
        Assert.Equal(handlers, probe.HandlerCalls);
    }

    private static string Header(HttpResponseMessage response, string name)
    {
        return Assert.Single(response.Headers.GetValues(name));
    }

    private static IEnumerable<RouteEndpoint> Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    }

    private static RouteEndpoint Route(WebApplication app, string pattern)
    {
        return Assert.Single(Routes(app), route => route.RoutePattern.RawText == pattern);
    }

    private static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(value => value.Kind + " " + value.Path)));
    }

    private static void MapNative(WebApplication app, bool enabled)
    {
        if (!enabled) { return; }

        var group = app.MapGroup("/access").WithTags("Access policies");
        group.WithMetadata(new AuthorizeAttribute { Policy = BrowserPolicies.Workspace }, new EnableCorsAttribute(BrowserPolicies.Cors));
        group.AddEndpointFilter((context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return next(context);
        });
        MapOriginal(group, "/read/{value:int}", new AccessReadEndpoint().HandleAsync, "GET").WithName("AccessRead");
        MapOriginal(group, "/value/{value:int}", new AccessValueEndpoint().HandleAsync, "POST").WithName("AccessValue");
        MapOriginal(group, "/value-alias/{value:int}", new AccessValueEndpoint().HandleAsync, "POST");
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler, string method)
    {
        return endpoints.MapMethods(pattern, new[] { method }, handler);
    }

    private static Ok<string> CompositionValue()
    {
        return TypedResults.Ok("fixed");
    }

    public sealed record CompositionStamp(string Kind);

    [EndpointResponse(418)]
    [ResponseHeader("X-Unused-Composition", "ignored")]
    public sealed class OuterConvention : IMediumApiEndpointConvention
    {
        private OuterConvention()
        {
            throw new InvalidOperationException("Nested conventions are not instantiated.");
        }

        public static void Configure(IEndpointConventionBuilder builder)
        {
            builder.WithMetadata(new CompositionStamp("before"));
            builder.AddMediumApiConvention<StructConvention>().AddMediumApiConvention<ExplicitConvention>();
            builder.WithMetadata(new CompositionStamp("after"));
        }
    }

    public readonly struct StructConvention : IMediumApiEndpointConvention
    {
        public static void Configure(IEndpointConventionBuilder builder)
        {
            builder.WithMetadata(new CompositionStamp("struct"));
        }
    }

    public sealed class ExplicitConvention : IMediumApiEndpointConvention
    {
        private ExplicitConvention()
        {
            throw new InvalidOperationException("Explicit static conventions are not instantiated.");
        }

        static void IMediumApiEndpointConvention.Configure(IEndpointConventionBuilder builder)
        {
            builder.WithMetadata(new CompositionStamp("explicit"));
        }
    }

    public readonly struct ThrowingConvention : IMediumApiEndpointConvention
    {
        public static InvalidOperationException Failure { get; } = new("A fixed nested convention failure.");

        public static void Configure(IEndpointConventionBuilder builder)
        {
            throw Failure;
        }
    }
}