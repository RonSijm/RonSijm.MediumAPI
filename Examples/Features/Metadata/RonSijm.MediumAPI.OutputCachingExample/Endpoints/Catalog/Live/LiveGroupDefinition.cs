using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;

namespace RonSijm.MediumAPI.OutputCachingExample.Endpoints.Catalog.Live;

[OutputCache(NoStore = true)]
public sealed class LiveGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/live");
}

internal sealed class GetLiveValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<CacheResponse> HandleAsync(CacheProbe probe) => TypedResults.Ok(probe.Record("live"));
}