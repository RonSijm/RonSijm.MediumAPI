using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.Endpoints.Disabled;

[OutputCache(PolicyName = CachePolicies.Catalog, Tags = new[] { CachePolicies.CatalogTag })]
public sealed class GetReenabledCachedValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/reenabled";

    public Ok<CacheResponse> HandleAsync(CacheProbe probe) => TypedResults.Ok(probe.Record("reenabled"));
}