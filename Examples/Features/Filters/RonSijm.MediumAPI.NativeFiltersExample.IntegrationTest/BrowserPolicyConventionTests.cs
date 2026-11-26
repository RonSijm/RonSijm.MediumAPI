using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.NativeFiltersExample.BrowserPolicyEndpoints;
using RonSijm.MediumAPI.NativeFiltersExample.BrowserPolicyEndpoints.Disabled;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.NativeFiltersExample.Program;

namespace RonSijm.MediumAPI.NativeFiltersExample.IntegrationTest;

public sealed class BrowserPolicyConventionTests
{
    [Theory]
    [InlineData(false, 401)]
    [InlineData(true, 403)]
    public async Task Native_authentication_and_authorization_reject_before_the_guard_and_headers(bool signedIn, int status)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var cookie = signedIn ? await SignIn(client, member: false) : null;
            using var response = await Send(client, HttpMethod.Get, "/browser/read/7", cookie, origin: BrowserPolicies.Origin);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal(BrowserPolicies.Origin, Header(response, "Access-Control-Allow-Origin"));
            AssertNoStore(response, false);
            AssertCounts(app, 0, 0, 0);
        }
    }

    [Fact]
    public async Task Authenticated_GET_bypasses_antiforgery_but_reaches_headers_and_the_handler()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, HttpMethod.Get, "/browser/read/7", await SignIn(client));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(7, (await response.Content.ReadFromJsonAsync<BrowserReceipt>())!.Value);
            AssertNoStore(response, true);
            AssertCounts(app, 1, 0, 1);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid-fixed-token")]
    public async Task Missing_or_invalid_native_antiforgery_tokens_reject_before_group_headers(string? token)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, HttpMethod.Post, "/browser/value/7", await SignIn(client), token);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AssertNoStore(response, false);
            var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>();
            Assert.Equal("antiforgery-failed", problem!["code"]!.GetValue<string>());
            AssertCounts(app, 1, 1, 0);
        }
    }

    [Theory]
    [InlineData(null, 200)]
    [InlineData("reject", 400)]
    [InlineData("failure", 500)]
    public async Task Valid_cookie_and_antiforgery_tokens_retain_headers_through_downstream_outcomes(string? outcome, int status)
    {
        string? referenceError = null;
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var session = await Session(app, client);
            using var response = await Send(client, HttpMethod.Post, "/browser/value/7" + (outcome is null ? "" : "?outcome=" + outcome), session.Cookie, session.Token, BrowserPolicies.Origin);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal(BrowserPolicies.Origin, Header(response, "Access-Control-Allow-Origin"));
            AssertNoStore(response, true);
            AssertCounts(app, 1, 1, 1);
            if (outcome is null) { Assert.Equal(7, (await response.Content.ReadFromJsonAsync<BrowserReceipt>())!.Value); }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                if (native) { referenceError = body; }
                else { Assert.Equal(referenceError, body); }
            }
        }
    }

    [Theory]
    [InlineData(BrowserPolicies.Origin, true)]
    [InlineData("https://other.example.test", false)]
    public async Task CORS_preflight_is_native_and_does_not_invoke_authorization_guards_or_headers(string origin, bool allowed)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Options, "/browser/value/7");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", BrowserPolicies.TokenHeader);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
            if (allowed)
            {
                Assert.Equal(origin, Header(response, "Access-Control-Allow-Origin"));
                Assert.Equal("true", Header(response, "Access-Control-Allow-Credentials"));
                Assert.Contains("POST", Header(response, "Access-Control-Allow-Methods"), StringComparison.Ordinal);
            }

            AssertNoStore(response, false);
            AssertCounts(app, 0, 0, 0);
        }
    }

    [Fact]
    public async Task A_disallowed_origin_does_not_turn_CORS_into_server_authorization()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, HttpMethod.Get, "/browser/read/7", await SignIn(client), origin: "https://other.example.test");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
            AssertNoStore(response, true);
            AssertCounts(app, 1, 0, 1);
        }
    }

    [Fact]
    public async Task Exact_native_policy_types_order_original_methods_responses_and_API_Explorer_are_preserved()
    {
        await using var native = Create(native: true);
        await using var app = Create();
        await native.StartAsync();
        await app.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(native.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot(), app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot()));
        foreach (var expected in Routes(native).Where(route => route.RoutePattern.RawText!.StartsWith("/browser", StringComparison.Ordinal)))
        {
            var actual = Route(app, expected.RoutePattern.RawText!);
            Assert.Equal(PolicyMetadata(expected), PolicyMetadata(actual));
            Assert.Equal(new[] { typeof(AuthorizeAttribute), typeof(EnableCorsAttribute) }, actual.Metadata.Where(value => value is IAuthorizeData or ICorsMetadata).Select(value => value.GetType()));
            Assert.Equal(expected.Metadata.GetMetadata<MethodInfo>(), actual.Metadata.GetMetadata<MethodInfo>());
        }

        Assert.Null(Route(app, "/browser/value-alias/{value:int}").Metadata.GetMetadata<IEndpointNameMetadata>());
    }

    [Fact]
    public async Task Repeated_typed_mounts_have_fresh_policy_objects_deliberate_names_and_the_same_native_pipeline()
    {
        await using var app = Create(map: false);
        foreach (var prefix in new[] { "/first", "/second" })
        {
            var group = app.MapGroup(prefix).MapMediumApiNamespace<BrowserPolicyGroupDefinition>();
            BrowserPolicyScenario.MapEntries(group, suppressNames: true);
        }

        await app.StartAsync();
        var first = Route(app, "/first/browser/read/{value:int}").Metadata;
        var second = Route(app, "/second/browser/read/{value:int}").Metadata;
        Assert.NotSame(first.GetMetadata<IAuthorizeData>(), second.GetMetadata<IAuthorizeData>());
        Assert.NotSame(first.GetMetadata<EnableCorsAttribute>(), second.GetMetadata<EnableCorsAttribute>());
        Assert.Same(first.GetMetadata<IAuthorizeData>(), Route(app, "/first/browser/value-alias/{value:int}").Metadata.GetMetadata<IAuthorizeData>());
        Assert.Null(first.GetMetadata<IEndpointNameMetadata>());
        using var client = app.GetTestClient();
        var cookie = await SignIn(client);
        foreach (var path in new[] { "/first/browser/read/7", "/second/browser/read/7" })
        {
            using var response = await Send(client, HttpMethod.Get, path, cookie);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AssertNoStore(response, true);
        }
    }

    [Fact]
    public async Task Disabled_namespace_does_not_run_its_convention_and_default_host_routes_remain_separate()
    {
        await using var app = Create(enabled: false);
        Assert.Null(app.TryMapMediumApiNamespace<DisabledBrowserGroupDefinition>());
        await app.StartAsync();
        Assert.DoesNotContain(Routes(app), route => route.RoutePattern.RawText!.Contains("/browser", StringComparison.Ordinal));
        await using var original = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await original.StartAsync();
        Assert.DoesNotContain(Routes(original), route => route.RoutePattern.RawText!.Contains("/browser", StringComparison.Ordinal));
    }

    internal static WebApplication Create(bool native = false, bool enabled = true, bool map = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(ExampleProgram).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddScoped<RequestTrace>();
        PolicyAdoptionScenario.AddServices(builder.Services);
        BrowserPolicyScenario.AddServices(builder.Services);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Sample:BrowserPolicies"] = enabled.ToString() });
        var app = builder.Build();
        BrowserPolicyScenario.UsePolicies(app);
        // This fixture issues only known synthetic principals. The runnable sample has no login endpoint.
        app.MapPost("/fixture/session/{permission}", FixtureSession).AllowAnonymous();
        if (map)
        {
            if (native) { MapNative(app, enabled); }
            else { BrowserPolicyScenario.Map(app); }
        }

        return app;
    }

    private static async Task<NoContent> FixtureSession(string permission, HttpContext context)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "synthetic-browser-user") };
        if (permission == "member") { claims.Add(new Claim("sample-permission", "workspace")); }

        await context.SignInAsync(DiagnosticPolicies.Scheme, new ClaimsPrincipal(new ClaimsIdentity(claims, DiagnosticPolicies.Scheme)));
        return TypedResults.NoContent();
    }

    private static async Task<string> SignIn(HttpClient client, bool member = true)
    {
        using var response = await client.PostAsync("/fixture/session/" + (member ? "member" : "visitor"), null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return Cookie(response);
    }

    private static async Task<BrowserSession> Session(WebApplication app, HttpClient client)
    {
        var cookie = await SignIn(client);
        using var response = await Send(client, HttpMethod.Get, "/browser/token", cookie);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<BrowserTokenResponse>())!.RequestToken;
        Assert.False(string.IsNullOrEmpty(token));
        cookie += "; " + Cookie(response);
        var probe = app.Services.GetRequiredService<BrowserPolicyProbe>();
        probe.GuardCalls = 0;
        probe.AntiforgeryChecks = 0;
        probe.HandlerCalls = 0;
        return new BrowserSession(cookie, token);
    }

    private static string Cookie(HttpResponseMessage response)
    {
        return string.Join("; ", response.Headers.GetValues("Set-Cookie").Select(value => value.Split(';')[0]));
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string path, string? cookie = null, string? token = null, string? origin = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (cookie is not null) { request.Headers.Add("Cookie", cookie); }

        if (token is not null) { request.Headers.Add(BrowserPolicies.TokenHeader, token); }

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

    private static void AssertNoStore(HttpResponseMessage response, bool expected)
    {
        Assert.Equal(expected, response.Headers.CacheControl?.NoStore == true);
        Assert.Equal(expected, response.Headers.Pragma.Any(value => value.Name == "no-cache"));
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

    private static string[] PolicyMetadata(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.Where(value => value is IAuthorizeData or ICorsMetadata).Select(value => value.GetType().FullName + ":" + (value is IAuthorizeData authorize ? authorize.Policy + ":" + authorize.Roles + ":" + authorize.AuthenticationSchemes : ((IEnableCorsAttribute)value).PolicyName)).ToArray();
    }

    private static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(value => value.Kind + " " + value.Path)));
    }

    private static void MapNative(WebApplication app, bool enabled)
    {
        if (!enabled) { return; }

        var group = app.MapGroup("/browser").WithTags("Browser policies");
        group.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var probe = http.RequestServices.GetRequiredService<BrowserPolicyProbe>();
            probe.GuardCalls++;
            if (!(HttpMethods.IsGet(http.Request.Method) || HttpMethods.IsHead(http.Request.Method) || HttpMethods.IsOptions(http.Request.Method) || HttpMethods.IsTrace(http.Request.Method)))
            {
                probe.AntiforgeryChecks++;
                if (!await http.RequestServices.GetRequiredService<IAntiforgery>().IsRequestValidAsync(http))
                {
                    return TypedResults.Problem(statusCode: 400, title: "A valid sample antiforgery token is required.", type: "urn:sample:antiforgery", extensions: new Dictionary<string, object?> { ["code"] = "antiforgery-failed" });
                }
            }

            return await next(context);
        });
        group.WithMetadata(new AuthorizeAttribute { Policy = BrowserPolicies.Workspace }, new EnableCorsAttribute(BrowserPolicies.Cors));
        group.AddEndpointFilter((context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            context.HttpContext.Response.Headers.Pragma = "no-cache";
            return next(context);
        });
        MapNativeHandler(group, "/read/{value:int}", new BrowserReadEndpoint().HandleAsync, "GET").WithName("BrowserRead");
        MapNativeHandler(group, "/token", new BrowserTokenEndpoint().HandleAsync, "GET").WithName("BrowserToken");
        MapNativeHandler(group, "/value/{value:int}", new BrowserValueEndpoint().HandleAsync, "POST").WithName("BrowserValue");
        MapNativeHandler(group, "/value-alias/{value:int}", new BrowserValueEndpoint().HandleAsync, "POST");
    }

    private static RouteHandlerBuilder MapNativeHandler(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler, string method)
    {
        return endpoints.MapMethods(pattern, new[] { method }, handler);
    }

    private sealed record BrowserSession(string Cookie, string Token);
}