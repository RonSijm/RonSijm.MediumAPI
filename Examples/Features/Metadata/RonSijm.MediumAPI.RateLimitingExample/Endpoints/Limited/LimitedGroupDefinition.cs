using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.RateLimitingExample.Endpoints.Limited;

[EnableRateLimiting(RateLimitPolicies.Parent)]
public sealed class LimitedGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/limited").RequireRateLimiting(RateLimitPolicies.Map);
        group.MapGet("/native", (RateLimitProbe probe) => SampleReplies.Reply("native", probe));
        return group;
    }
}

internal sealed class GetLimitedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("limited", probe);
}

internal sealed class GetEndpointOverrideEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method";

    [EnableRateLimiting(RateLimitPolicies.Endpoint)]
    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("method", probe);
}