namespace RonSijm.MediumAPI.AntiforgeryExample.Endpoints;

public sealed class AntiforgeryApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/antiforgery");
}