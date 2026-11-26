using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ExceptionFiltersExample.Endpoints.ClassFilters;

internal sealed class ClassFiltersGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api/class");
}

[EndpointFilter<ProductExceptionFilter>]
internal sealed class GetClassFilteredProductEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/{id:int}";

    public async Task<Ok<ProductResponse>> HandleAsync([FromRoute] int id, [FromServices] LegacyProductCatalog products, CancellationToken cancellationToken) => TypedResults.Ok(await products.LoadAsync(id, cancellationToken));
}