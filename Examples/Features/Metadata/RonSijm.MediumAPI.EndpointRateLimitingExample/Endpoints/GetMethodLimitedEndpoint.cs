using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.Endpoints;

[EnableRateLimiting(RateLimitPolicies.Class)]
public sealed class GetMethodLimitedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method";

    [EnableRateLimiting(RateLimitPolicies.Method)]
    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("method", probe);
}