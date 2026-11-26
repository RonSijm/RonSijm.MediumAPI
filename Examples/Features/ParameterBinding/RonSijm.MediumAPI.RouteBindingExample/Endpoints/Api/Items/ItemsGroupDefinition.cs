namespace RonSijm.MediumAPI.RouteBindingExample.Endpoints.Api.Items;

internal sealed class ItemsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => OpenApiRouteHandlerBuilderExtensions.WithDescription<RouteGroupBuilder>(description: "Items under the inherited customer route", builder: endpoints.MapGroup("/items"));
}