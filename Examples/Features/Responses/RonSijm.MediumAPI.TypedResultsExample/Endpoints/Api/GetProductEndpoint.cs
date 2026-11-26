using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.TypedResultsExample.Endpoints.Api;

// Multiple bound inputs keep the non-generic verb interface. The method still
// exposes its complete HTTP result union to the existing handler adapter.
internal sealed class GetProductEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/products/{id:int}";

    [EndpointName("TypedGetProduct")]
    [EndpointSummary("Read one synthetic product")]
    public async Task<Results<Ok<ProductResponse>, NotFound, BadRequest>> HandleAsync([FromRoute] int id, [FromServices] ProductCatalog products, CancellationToken cancellationToken, [FromQuery] bool includeDescription = true)
    {
        if (id <= 0)
        {
            return TypedResults.BadRequest();
        }

        var product = await products.FindAsync(id, cancellationToken);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(includeDescription ? product : product with { Description = null });
    }
}