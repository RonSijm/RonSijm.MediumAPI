using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.Endpoints;

[EnableRateLimiting(RateLimitPolicies.Class)]
public sealed class GetConfiguredLimitedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/configured";

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.RequireRateLimiting(RateLimitPolicies.Fluent);

    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("configured", probe);
}