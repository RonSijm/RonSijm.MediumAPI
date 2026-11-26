using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.OpenApiMigrationExample;

internal static class NativeMappings
{
    internal static void Map(WebApplication app)
    {
        var api = app.MapGroup("/api").WithTags(SampleMetadata.CatalogTag);
        api.MapGet("/products/{productId:long}", GetProduct).WithName("GetProduct").WithSummary(SampleMetadata.GetSummary).WithDescription(SampleMetadata.GetDescription);
        api.MapPost("/products", CreateProduct).WithName("CreateProduct").WithSummary(SampleMetadata.CreateSummary).WithDescription(SampleMetadata.CreateDescription);
        api.MapGet("/internal-status", GetInternalStatus).WithName("GetInternalStatus").ExcludeFromDescription();
        app.MapGet("/compat/products/{productId:long}", GetProduct).WithTags(SampleMetadata.CatalogTag).WithSummary(SampleMetadata.GetSummary).WithDescription(SampleMetadata.GetDescription);
    }

    private static Results<Ok<ProductResponse>, NotFound> GetProduct([FromRoute(Name = "productId")] long id, [FromQuery] string? search, [FromHeader(Name = SampleMetadata.TraceHeader)] string? trace, HttpContext context, [FromServices] CatalogService catalog, CancellationToken cancellationToken, [FromQuery] int take = 10)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        cancellationToken.ThrowIfCancellationRequested();
        var product = catalog.Find(id, search, trace, take);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(product);
    }

    private static Results<Created<ProductResponse>, BadRequest<ValidationResponse>> CreateProduct([FromBody] CreateProductRequest create, [FromServices] CatalogService catalog, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(create.Name))
        {
            return TypedResults.BadRequest(new ValidationResponse("name-required"));
        }

        var product = catalog.Create(create);
        return TypedResults.Created($"/api/products/{product.Id}", product);
    }

    private static Ok<InternalStatusResponse> GetInternalStatus() => TypedResults.Ok(new InternalStatusResponse("ready"));
}