using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ExceptionFiltersExample.Endpoints.Products;

internal sealed class GetProductEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/{id:int}";

    [EndpointName("ExceptionFiltersGetProduct")]
    public async Task<Ok<ProductResponse>> HandleAsync([FromRoute] int id, [FromServices] LegacyProductCatalog products, CancellationToken cancellationToken) => TypedResults.Ok(await products.LoadAsync(id, cancellationToken));
}