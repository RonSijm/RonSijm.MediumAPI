using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.Endpoints;

[OutputCache]
public sealed class GetFluentCachedValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/fluent";

    public Ok<CacheResponse> HandleAsync(CacheProbe probe) => TypedResults.Ok(probe.Record("fluent"));

    // Native attribute policy runs after the separate fluent policy and can re-enable caching.
    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.CacheOutput(policy => policy.NoCache());
}