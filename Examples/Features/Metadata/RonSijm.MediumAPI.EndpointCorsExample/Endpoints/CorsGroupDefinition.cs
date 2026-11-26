namespace RonSijm.MediumAPI.EndpointCorsExample.Endpoints;

public sealed class CorsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/cors");
}