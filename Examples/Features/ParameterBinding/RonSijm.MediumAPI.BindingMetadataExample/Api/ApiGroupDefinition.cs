namespace RonSijm.MediumAPI.BindingMetadataExample.Api;

internal sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api");
}