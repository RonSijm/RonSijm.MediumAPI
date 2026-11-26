using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.RateLimitingExample.Endpoints.Disabled;

[EnableRateLimiting(RateLimitPolicies.Missing)]
public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        context.Services.GetRequiredService<RateLimitProbe>().DisabledConditions++;
        return false;
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.ServiceProvider.GetRequiredService<RateLimitProbe>().DisabledMaps++;
        throw new InvalidOperationException("The disabled sample namespace must not map.");
    }
}

internal sealed class GetDisabledEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("disabled", probe);
}