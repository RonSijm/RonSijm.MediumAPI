using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.RateLimitingExample.Endpoints.Limited.Child;

[EnableRateLimiting(RateLimitPolicies.Child)]
public sealed class ChildGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/child");
}

internal sealed class GetChildEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("child", probe);
}