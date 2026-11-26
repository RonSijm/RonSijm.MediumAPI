using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;

namespace RonSijm.MediumAPI.OutputCachingExample.Endpoints;

internal sealed class EvictCatalogCacheEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/cache/evict-catalog";

    public async Task<NoContent> HandleAsync(IOutputCacheStore store, CancellationToken cancellationToken)
    {
        await store.EvictByTagAsync(CachePolicies.CatalogTag, cancellationToken);
        return TypedResults.NoContent();
    }
}