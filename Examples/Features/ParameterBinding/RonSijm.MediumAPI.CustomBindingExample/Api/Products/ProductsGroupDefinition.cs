namespace RonSijm.MediumAPI.CustomBindingExample.Api.Products;

internal sealed class ProductsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/products/{productId:long}");
}