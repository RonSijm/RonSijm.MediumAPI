using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.Endpoints;

[OutputCache]
public sealed class GetErrorValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/error";

    public BadRequest<CacheResponse> HandleAsync(CacheProbe probe) => TypedResults.BadRequest(probe.Record("error"));
}