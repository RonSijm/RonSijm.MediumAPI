namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.Endpoints.Disabled;

internal sealed class DisabledGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/group-disabled").DisableAntiforgery();
}