namespace RonSijm.MediumAPI.DeclarationsExample.Api;

internal sealed partial class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api");
}