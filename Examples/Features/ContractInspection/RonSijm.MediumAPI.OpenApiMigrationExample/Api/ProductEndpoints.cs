using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.OpenApiMigrationExample.Api;

[Tags(SampleMetadata.CatalogTag)]
internal sealed class CatalogGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api");
}

internal sealed class GetProductEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/products/{productId:long}";

    [EndpointSummary(SampleMetadata.GetSummary)]
    [EndpointDescription(SampleMetadata.GetDescription)]
    [ResponseHeader("CacheControl", "no-store")]
    [ResponseHeader("Pragma", "no-cache")]
    public Results<Ok<ProductResponse>, NotFound> HandleAsync([FromRoute(Name = "productId")] long id, [FromQuery] string? search, [FromHeader(Name = SampleMetadata.TraceHeader)] string? trace, HttpContext context, [FromServices] CatalogService catalog, CancellationToken cancellationToken, [FromQuery] int take = 10)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var product = catalog.Find(id, search, trace, take);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(product);
    }
}

internal sealed class CreateProductEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/products";

    [EndpointSummary(SampleMetadata.CreateSummary)]
    [EndpointDescription(SampleMetadata.CreateDescription)]
    public Results<Created<ProductResponse>, BadRequest<ValidationResponse>> HandleAsync([FromBody] CreateProductRequest create, [FromServices] CatalogService catalog, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(create.Name))
        {
            return TypedResults.BadRequest(new ValidationResponse("name-required"));
        }

        var product = catalog.Create(create);
        return TypedResults.Created($"/api/products/{product.Id}", product);
    }
}

internal sealed class GetInternalStatusEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/internal-status";

    [ExcludeFromDescription]
    public Ok<InternalStatusResponse> HandleAsync() => TypedResults.Ok(new InternalStatusResponse("ready"));
}