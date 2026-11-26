using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.Endpoints;

[OutputCache]
public sealed class GetCookieValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/cookie";

    public Ok<CacheResponse> HandleAsync(HttpContext context, CacheProbe probe)
    {
        context.Response.Cookies.Append(CachePolicies.CookieName, "synthetic-value");
        return TypedResults.Ok(probe.Record("cookie"));
    }
}