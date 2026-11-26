using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.Endpoints.Enabled;

[EnableRateLimiting(RateLimitPolicies.Group)]
public sealed class EnabledGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/enabled-group");
}