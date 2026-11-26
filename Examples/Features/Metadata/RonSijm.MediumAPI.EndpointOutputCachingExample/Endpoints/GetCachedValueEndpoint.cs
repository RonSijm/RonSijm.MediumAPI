using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.Endpoints;

[OutputCache(PolicyName = CachePolicies.Catalog, Duration = 600, VaryByQueryKeys = new[] { "category" }, VaryByHeaderNames = new[] { CachePolicies.LanguageHeader }, Tags = new[] { CachePolicies.CatalogTag })]
public sealed class GetCachedValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<CacheResponse> HandleAsync(CacheProbe probe, [FromQuery] string? category, [FromHeader(Name = CachePolicies.LanguageHeader)] string? language) => TypedResults.Ok(probe.Record("value", category, language));
}