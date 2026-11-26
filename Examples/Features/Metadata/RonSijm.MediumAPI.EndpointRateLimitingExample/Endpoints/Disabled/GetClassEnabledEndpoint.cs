using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.Endpoints.Disabled;

[EnableRateLimiting(RateLimitPolicies.Class)]
public sealed class GetClassEnabledEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-enabled";

    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("class-enabled", probe);
}