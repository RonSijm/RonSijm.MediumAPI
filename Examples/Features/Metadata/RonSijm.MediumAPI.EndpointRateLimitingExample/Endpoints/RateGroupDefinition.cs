namespace RonSijm.MediumAPI.EndpointRateLimitingExample.Endpoints;

public sealed class RateGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/rate");
}