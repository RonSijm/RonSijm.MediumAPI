using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.Endpoints.Enabled;

[DisableRateLimiting]
public sealed class GetClassDisabledEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-disabled";

    [EnableRateLimiting(RateLimitPolicies.Method)]
    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("class-disabled", probe);
}