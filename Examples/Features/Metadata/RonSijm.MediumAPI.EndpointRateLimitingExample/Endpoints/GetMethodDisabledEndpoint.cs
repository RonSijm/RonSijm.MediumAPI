using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.Endpoints;

[EnableRateLimiting(RateLimitPolicies.Class)]
public sealed class GetMethodDisabledEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-enabled-method-disabled";

    [DisableRateLimiting]
    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("method-disabled", probe);
}