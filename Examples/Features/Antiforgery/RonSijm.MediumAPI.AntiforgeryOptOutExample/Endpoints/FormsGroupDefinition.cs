namespace RonSijm.MediumAPI.AntiforgeryOptOutExample.Endpoints;

public sealed class FormsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/opt-out");
}