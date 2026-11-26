using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.Endpoints;

[OutputCache]
public sealed class GetDefaultCachedValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/default";

    public Ok<CacheResponse> HandleAsync(CacheProbe probe) => TypedResults.Ok(probe.Record("default"));
}