using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.Endpoints;

[EnableRateLimiting(RateLimitPolicies.Class)]
public sealed class GetClassPeerEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-peer";

    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("class-peer", probe);
}