namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.Endpoints;

internal sealed class AntiforgeryGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/endpoint-antiforgery");
}