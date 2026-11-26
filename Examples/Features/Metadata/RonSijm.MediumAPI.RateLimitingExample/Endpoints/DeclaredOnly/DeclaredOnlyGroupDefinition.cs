using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.RateLimitingExample.Endpoints.DeclaredOnly;

[EnableRateLimiting(RateLimitPolicies.Missing)]
[DisableRateLimiting]
public abstract class RateLimitBase { }

public sealed class DeclaredOnlyGroupDefinition : RateLimitBase, INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/declared-only");
}

internal sealed class GetDeclaredOnlyEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("declared-only", probe);
}