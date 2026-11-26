using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ExceptionFiltersExample.Endpoints.Fluent;

internal sealed class FluentGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api/fluent");
}

internal sealed class GetFluentFilteredProductEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/{id:int}";

    public async Task<Ok<ProductResponse>> HandleAsync([FromRoute] int id, [FromServices] LegacyProductCatalog products, CancellationToken cancellationToken) => TypedResults.Ok(await products.LoadAsync(id, cancellationToken));

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.AddMediumApiFilter<ProductExceptionFilter>();
}