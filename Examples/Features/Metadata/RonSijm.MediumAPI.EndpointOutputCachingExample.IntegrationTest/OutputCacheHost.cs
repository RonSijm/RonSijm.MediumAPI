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
using ExampleProgram = RonSijm.MediumAPI.EndpointOutputCachingExample.Program;

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest;

internal static class OutputCacheHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true, bool useOutputCache = true, bool authenticated = false, Action<WebApplicationBuilder>? configure = null)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            configure?.Invoke(builder);
        }, mapEndpoints: mapEndpoints && !native, useOutputCache: useOutputCache && !authenticated);
        if (authenticated)
        {
            // Characterize native eligibility with a fixed synthetic principal before the cache middleware.
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

    private static void MapNativeReference(WebApplication app)
    {
        var root = app.MapGroup("/output-cache");
        root.MapGroup("").WithMetadata(new OutputCacheAttribute { PolicyName = CachePolicies.Catalog, Duration = 600, VaryByQueryKeys = ["category"], VaryByHeaderNames = [CachePolicies.LanguageHeader], Tags = [CachePolicies.CatalogTag] }).MapGet("/value", (CacheProbe probe, [FromQuery] string? category, [FromHeader(Name = CachePolicies.LanguageHeader)] string? language) => TypedResults.Ok(probe.Record("value", category, language))).WithName("GetCachedValue");
        root.MapGroup("").WithMetadata(new OutputCacheAttribute()).MapGet("/default", (CacheProbe probe) => TypedResults.Ok(probe.Record("default"))).WithName("GetDefaultCachedValue");
        root.MapGroup("").WithMetadata(new OutputCacheAttribute { PolicyName = CachePolicies.Catalog, Duration = 600 }).MapGet("/method", NativeMethod).WithName("GetMethodLiveValue");
        var disabled = root.MapGroup("/disabled").WithMetadata(new OutputCacheAttribute { NoStore = true });
        disabled.MapGroup("").WithMetadata(new OutputCacheAttribute { PolicyName = CachePolicies.Catalog, Tags = [CachePolicies.CatalogTag] }).MapGet("/reenabled", (CacheProbe probe) => TypedResults.Ok(probe.Record("reenabled"))).WithName("GetReenabledCachedValue");
        root.MapGroup("").WithMetadata(new OutputCacheAttribute()).MapGet("/fluent", (CacheProbe probe) => TypedResults.Ok(probe.Record("fluent"))).CacheOutput(policy => policy.NoCache()).WithName("GetFluentCachedValue");
        root.MapGroup("").WithMetadata(new OutputCacheAttribute()).MapGet("/cookie", (HttpContext context, CacheProbe probe) =>
        {
            context.Response.Cookies.Append(CachePolicies.CookieName, "synthetic-value");
            return TypedResults.Ok(probe.Record("cookie"));
        }).WithName("GetCookieValue");
        root.MapGroup("").WithMetadata(new OutputCacheAttribute()).MapGet("/error", (CacheProbe probe) => TypedResults.BadRequest(probe.Record("error"))).WithName("GetErrorValue");
        root.MapPost("/evict-catalog", async (IOutputCacheStore store, CancellationToken cancellationToken) =>
        {
            await store.EvictByTagAsync(CachePolicies.CatalogTag, cancellationToken);
            return TypedResults.NoContent();
        }).WithName("EvictCatalogCache");
    }

    [OutputCache(NoStore = true)]
    private static Ok<CacheResponse> NativeMethod(CacheProbe probe) => TypedResults.Ok(probe.Record("method"));
}