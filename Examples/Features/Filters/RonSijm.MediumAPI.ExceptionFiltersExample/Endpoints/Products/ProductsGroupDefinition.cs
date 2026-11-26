namespace RonSijm.MediumAPI.ExceptionFiltersExample.Endpoints.Products;

[EndpointFilter<ProductExceptionFilter>]
internal sealed class ProductsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api/products");
}