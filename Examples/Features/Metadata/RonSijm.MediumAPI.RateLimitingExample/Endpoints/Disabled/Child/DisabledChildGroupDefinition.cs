using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.RateLimitingExample.Endpoints.Disabled.Child;

[DisableRateLimiting]
public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        context.Services.GetRequiredService<RateLimitProbe>().DisabledChildConditions++;
        return true;
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.ServiceProvider.GetRequiredService<RateLimitProbe>().DisabledChildMaps++;
        throw new InvalidOperationException("A descendant of the disabled sample namespace must not map.");
    }
}

internal sealed class GetDisabledChildEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => SampleReplies.Reply("disabled-child", probe);
}