using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.OutputCachingExample.Program;

namespace RonSijm.MediumAPI.OutputCachingExample.IntegrationTest;

internal static class OutputCacheHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true, bool useOutputCache = true, bool authenticated = false)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Services.AddSingleton<FixtureProbe>();
        }, mapEndpoints: mapEndpoints && !native, useOutputCache: useOutputCache && !authenticated);
        if (authenticated)
        {
            app.Use((context, next) =>
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "synthetic-reader")], "Synthetic"));
                return next(context);
            });
            if (useOutputCache)
            {
                app.UseOutputCache();
            }
        }

        if (native && mapEndpoints)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static CacheProbe Probe(WebApplication app) => app.Services.GetRequiredService<CacheProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    internal static string[] AttributeInventory(WebApplication app) => Routes(app).Select(endpoint => string.Join("|", endpoint.RoutePattern.RawText, string.Join(";", endpoint.Metadata.GetOrderedMetadata<OutputCacheAttribute>().Select(attribute => string.Join(":", attribute.PolicyName, attribute.Duration, attribute.NoStore, string.Join(",", attribute.VaryByQueryKeys ?? []), string.Join(",", attribute.VaryByHeaderNames ?? []), string.Join(",", attribute.VaryByRouteValueNames ?? []), string.Join(",", attribute.Tags ?? [])))))).OrderBy(value => value, StringComparer.Ordinal).ToArray();

    internal static async Task<CacheResponse> Read(HttpClient client, string path, string? language = null, string? authorization = null, HttpStatusCode expectedStatus = HttpStatusCode.OK)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (language is not null)
        {
            request.Headers.Add(CachePolicies.LanguageHeader, language);
        }

        if (authorization is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorization);
        }

        using var response = await client.SendAsync(request);
        Assert.Equal(expectedStatus, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CacheResponse>())!;
    }

    internal static async Task<FixtureResponse> ReadFixture(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<FixtureResponse>())!;
    }

    internal static RouteGroupBuilder NativeCatalog(IEndpointRouteBuilder endpoints)
        => endpoints.MapGroup("/catalog").WithMetadata(new OutputCacheAttribute { PolicyName = CachePolicies.Catalog, Duration = 600, VaryByQueryKeys = ["category"], VaryByHeaderNames = [CachePolicies.LanguageHeader], Tags = [CachePolicies.CatalogTag] });

    internal static void MapNativeReference(WebApplication app, Action<RouteGroupBuilder>? configureCatalog = null)
    {
        var catalog = NativeCatalog(app);
        configureCatalog?.Invoke(catalog);
        catalog.MapGet("/value", (CacheProbe probe, [FromQuery] string? category, [FromHeader(Name = CachePolicies.LanguageHeader)] string? language) => TypedResults.Ok(probe.Record("catalog", category, language))).WithName("GetCatalogValue");
        catalog.MapGet("/method", NativeMethod).WithName("GetMethodValue");
        catalog.MapGet("/cookie", (HttpContext context, CacheProbe probe) =>
        {
            context.Response.Cookies.Append(CachePolicies.CookieName, "sample-value");
            return TypedResults.Ok(probe.Record("cookie"));
        }).WithName("GetCookieValue");
        catalog.MapGet("/error", (CacheProbe probe) => TypedResults.BadRequest(probe.Record("error"))).WithName("GetErrorValue");
        var live = catalog.MapGroup("/live").WithMetadata(new OutputCacheAttribute { NoStore = true });
        live.MapGet("/value", (CacheProbe probe) => TypedResults.Ok(probe.Record("live"))).WithName("GetLiveValue");
        var reenabled = live.MapGroup("/reenabled").WithMetadata(new OutputCacheAttribute { PolicyName = CachePolicies.Catalog, Duration = 600, Tags = [CachePolicies.CatalogTag] });
        reenabled.MapGet("/value", (CacheProbe probe) => TypedResults.Ok(probe.Record("reenabled"))).WithName("GetReenabledValue");
        var declaredOnly = app.MapGroup("/declared-only");
        declaredOnly.MapGet("/value", (CacheProbe probe) => TypedResults.Ok(probe.Record("declared-only"))).WithName("GetDeclaredOnlyValue");
        var defaultCache = app.MapGroup("/default").WithMetadata(new OutputCacheAttribute());
        defaultCache.MapGet("/value", (CacheProbe probe) => TypedResults.Ok(probe.Record("default"))).WithName("GetDefaultValue");
        var cache = app.MapGroup("/cache");
        cache.MapPost("/evict-catalog", async (IOutputCacheStore store, CancellationToken cancellationToken) =>
        {
            await store.EvictByTagAsync(CachePolicies.CatalogTag, cancellationToken);
            return TypedResults.NoContent();
        }).WithName("EvictCatalogCache");
    }

    [OutputCache(NoStore = true)]
    private static Ok<CacheResponse> NativeMethod(CacheProbe probe) => TypedResults.Ok(probe.Record("method"));
}