using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;

namespace RonSijm.MediumAPI.OutputCachingExample.Endpoints.Catalog.Live.Reenabled;

[OutputCache(PolicyName = CachePolicies.Catalog, Duration = 600, Tags = new[] { CachePolicies.CatalogTag })]
public sealed class ReenabledGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/reenabled");
}

internal sealed class GetReenabledValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<CacheResponse> HandleAsync(CacheProbe probe) => TypedResults.Ok(probe.Record("reenabled"));
}