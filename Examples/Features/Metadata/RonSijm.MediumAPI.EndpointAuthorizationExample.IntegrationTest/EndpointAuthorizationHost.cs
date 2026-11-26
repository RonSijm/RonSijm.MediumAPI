using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.EndpointAuthorizationExample.Program;

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest;

internal static class EndpointAuthorizationHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true, bool includeSignIn = false, Action<WebApplicationBuilder>? configure = null)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            configure?.Invoke(builder);
        }, mapEndpoints: mapEndpoints && !native);
        if (native && mapEndpoints)
        {
            MapNativeReference(app);
        }

        if (includeSignIn)
        {
            MapSignInFixture(app);
        }

        return app;
    }

    internal static AuthorizationProbe Probe(WebApplication app) => app.Services.GetRequiredService<AuthorizationProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    internal static string[] AuthorizationInventory(WebApplication app) => Routes(app).Select(endpoint => endpoint.RoutePattern.RawText + "|" + string.Join(";", endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(data => string.Join(":", data.Policy, data.Roles, data.AuthenticationSchemes))) + "|" + (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)).OrderBy(value => value, StringComparer.Ordinal).ToArray();

    internal static async Task<string> Issue(HttpClient client, string identity, string scheme = SamplePolicies.PrimaryScheme)
    {
        using var response = await client.PostAsync("/_test/cookies/" + scheme + "/" + identity, content: null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookieName = scheme == SamplePolicies.PrimaryScheme ? SamplePolicies.PrimaryCookie : SamplePolicies.SecondaryCookie;
        return Assert.Single(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith(cookieName + "=", StringComparison.Ordinal)).Split(';', 2)[0];
    }

    internal static async Task<HttpResponseMessage> Send(HttpClient client, string path, string? cookie = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", cookie);
        }

        return await client.SendAsync(request);
    }

    internal static async Task<AuthorizationReceipt> Read(HttpClient client, string path, string? cookie = null)
    {
        using var response = await Send(client, path, cookie);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthorizationReceipt>())!;
    }

    private static void MapNativeReference(WebApplication app)
    {
        var api = app.MapGroup("/auth");
        api.MapGet("/open", (HttpContext context, AuthorizationProbe probe) => NativeReceipt(context, probe, "open")).WithName("GetOpenAuthorization");
        api.MapGroup("").WithMetadata(new AuthorizeAttribute()).MapGet("/default", (HttpContext context, AuthorizationProbe probe) => NativeReceipt(context, probe, "default")).WithName("GetDefaultAuthorization");
        api.MapGroup("").WithMetadata(new AuthorizeAttribute(SamplePolicies.Readers)).MapGet("/read", (HttpContext context, AuthorizationProbe probe) => NativeReceipt(context, probe, "read")).WithName("GetReadAuthorization");
        api.MapGroup("").WithMetadata(new AuthorizeAttribute { Roles = "Reader,Administrator", AuthenticationSchemes = SamplePolicies.SecondaryScheme }).MapGet("/roles", (HttpContext context, AuthorizationProbe probe) => NativeReceipt(context, probe, "roles")).WithName("GetRoleAuthorization");
        api.MapGroup("").WithMetadata(new AuthorizeAttribute(SamplePolicies.Readers)).MapGet("/public-method", NativePublicMethod).WithName("GetPublicMethodAuthorization");
        api.MapGroup("").WithMetadata(new AuthorizeAttribute(SamplePolicies.Readers)).MapGet("/property", (HttpContext context, AuthorizationProbe probe) => NativeReceipt(context, probe, "property")).RequireAuthorization(SamplePolicies.Writers).RequireAuthorization(SamplePolicies.Audit).WithName("GetPropertyAuthorization");
        var protectedGroup = api.MapGroup("/protected").WithMetadata(new AuthorizeAttribute(SamplePolicies.GroupAccess));
        protectedGroup.MapGroup("").WithMetadata(new AuthorizeAttribute(SamplePolicies.Readers)).MapGet("/combined", NativeCombined).WithName("GetCombinedAuthorization");
        protectedGroup.MapGroup("").WithMetadata(new AllowAnonymousAttribute()).MapGet("/public-class", NativePublicClass).WithName("GetPublicClassAuthorization");
    }

    private static Ok<AuthorizationReceipt> NativeReceipt(HttpContext context, AuthorizationProbe probe, string handler) => TypedResults.Ok(probe.Record(handler, context.User.Identity?.IsAuthenticated == true));

    [AllowAnonymous]
    private static Ok<AuthorizationReceipt> NativePublicMethod(HttpContext context, AuthorizationProbe probe) => NativeReceipt(context, probe, "public-method");

    [Authorize(SamplePolicies.Writers)]
    private static Ok<AuthorizationReceipt> NativeCombined(HttpContext context, AuthorizationProbe probe) => NativeReceipt(context, probe, "combined");

    [Authorize(SamplePolicies.Readers)]
    private static Ok<AuthorizationReceipt> NativePublicClass(HttpContext context, AuthorizationProbe probe) => NativeReceipt(context, probe, "public-class");

    private static void MapSignInFixture(WebApplication app)
    {
        // Test-only fixture: real CookieAuthentication signs fixed synthetic principals.
        // The runnable example intentionally contains no login or identity implementation.
        app.MapPost("/_test/cookies/{scheme}/{identity}", async (string scheme, string identity, HttpContext context) =>
        {
            if (scheme is not (SamplePolicies.PrimaryScheme or SamplePolicies.SecondaryScheme))
            {
                throw new InvalidOperationException("Unknown synthetic cookie scheme.");
            }

            await context.SignInAsync(scheme, Principal(identity, scheme));
            return TypedResults.NoContent();
        }).AllowAnonymous().ExcludeFromDescription();
    }

    private static ClaimsPrincipal Principal(string identity, string scheme)
    {
        if (identity is not ("member" or "reader" or "writer" or "reader-writer" or "catalog-reader" or "complete" or "administrator"))
        {
            throw new InvalidOperationException("Unknown synthetic identity.");
        }

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "synthetic-identity") };
        if (identity is "reader" or "reader-writer" or "catalog-reader" or "complete" or "administrator")
        {
            claims.Add(new Claim(SamplePolicies.PermissionClaim, "read"));
        }

        if (identity is "writer" or "reader-writer" or "complete")
        {
            claims.Add(new Claim(SamplePolicies.PermissionClaim, "write"));
        }

        if (identity is "catalog-reader" or "complete")
        {
            claims.Add(new Claim("module", "catalog"));
        }

        if (identity is "complete")
        {
            claims.Add(new Claim("audit", "allowed"));
            claims.Add(new Claim("fallback", "allowed"));
        }

        if (identity is "reader" or "complete")
        {
            claims.Add(new Claim(ClaimTypes.Role, "Reader"));
        }

        if (identity is "administrator")
        {
            claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme));
    }
}