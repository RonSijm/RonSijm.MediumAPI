using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.Endpoints;

[EnableRateLimiting(RateLimitPolicies.Class)]
public sealed class GetClassLimitedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class";

    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("class", probe);
}