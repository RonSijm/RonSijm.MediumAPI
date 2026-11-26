using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.RateLimitingExample.Endpoints.Unlimited.Child;

// This name is deliberately aspirational: a native enable marker cannot cancel
// the parent's disable marker. The middleware bypasses limiting for this route.
[EnableRateLimiting(RateLimitPolicies.Parent)]
public sealed class ReenabledGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/child");
}

internal sealed class GetReenabledEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("unlimited-child", probe);
}