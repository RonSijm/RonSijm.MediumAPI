using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ExceptionFiltersExample.Endpoints.MethodFilters;

internal sealed class MethodFiltersGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api/method");
}

// The class marker is deliberately not inherited. The handler marker is inherited
// by an override, using the same method-attribute rules as the other handler metadata.
[EndpointFilter<MustStayDisabledFilter>]
internal abstract class ProductHandlerBase
{
    [EndpointFilter<ProductExceptionFilter>]
    public virtual async Task<Ok<ProductResponse>> HandleAsync([FromRoute] int id, [FromServices] LegacyProductCatalog products, CancellationToken cancellationToken) => TypedResults.Ok(await products.LoadAsync(id, cancellationToken));
}

internal sealed class GetMethodFilteredProductEndpoint : ProductHandlerBase, IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/{id:int}";

    public override Task<Ok<ProductResponse>> HandleAsync([FromRoute] int id, [FromServices] LegacyProductCatalog products, CancellationToken cancellationToken) => base.HandleAsync(id, products, cancellationToken);
}