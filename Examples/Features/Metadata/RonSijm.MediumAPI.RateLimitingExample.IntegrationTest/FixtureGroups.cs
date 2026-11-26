using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.RateLimitingExample.IntegrationTest.Fixtures
{
    public sealed record StageMarker(string Stage);
}

namespace RonSijm.MediumAPI.RateLimitingExample.IntegrationTest.Fixtures.Missing
{
    [EnableRateLimiting(RateLimitPolicies.Missing)]
    public sealed class MissingGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/missing");
    }

    public sealed class MissingPolicyEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => RateLimitHost.Reply(probe, "missing");
    }
}

namespace RonSijm.MediumAPI.RateLimitingExample.IntegrationTest.Fixtures.Ordered
{
    [EnableRateLimiting(RateLimitPolicies.Parent)]
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/ordered").RequireRateLimiting(RateLimitPolicies.Map).WithMetadata(new StageMarker("Map"));
    }

    public sealed class OrderedEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [EnableRateLimiting(RateLimitPolicies.Endpoint)]
        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => RateLimitHost.Reply(probe, "ordered");

        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithMetadata(new StageMarker("Endpoint"));
    }
}