using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace RonSijm.MediumAPI.OutputCachingExample.Endpoints.Catalog;

[OutputCache(PolicyName = CachePolicies.Catalog, Duration = 600, VaryByQueryKeys = new[] { "category" }, VaryByHeaderNames = new[] { CachePolicies.LanguageHeader }, Tags = new[] { CachePolicies.CatalogTag })]
public sealed class CatalogGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/catalog");
}

internal sealed class GetCatalogValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<CacheResponse> HandleAsync(CacheProbe probe, [FromQuery] string? category, [FromHeader(Name = CachePolicies.LanguageHeader)] string? language) => TypedResults.Ok(probe.Record("catalog", category, language));
}

internal sealed class GetMethodValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method";

    [OutputCache(NoStore = true)]
    public Ok<CacheResponse> HandleAsync(CacheProbe probe) => TypedResults.Ok(probe.Record("method"));
}

internal sealed class GetCookieValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/cookie";

    public Ok<CacheResponse> HandleAsync(HttpContext context, CacheProbe probe)
    {
        context.Response.Cookies.Append(CachePolicies.CookieName, "sample-value");
        return TypedResults.Ok(probe.Record("cookie"));
    }
}

internal sealed class GetErrorValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/error";

    public BadRequest<CacheResponse> HandleAsync(CacheProbe probe) => TypedResults.BadRequest(probe.Record("error"));
}