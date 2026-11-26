namespace RonSijm.MediumAPI.EndpointAuthorizationExample.Endpoints;

public sealed class AuthorizationGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/auth");
}