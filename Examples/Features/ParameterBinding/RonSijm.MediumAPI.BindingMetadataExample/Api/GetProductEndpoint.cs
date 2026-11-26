using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI.BindingMetadataExample.Bindings;

namespace RonSijm.MediumAPI.BindingMetadataExample.Api;

internal sealed class GetProductEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/products/{id:long}";

    public Ok<ProductResponse> HandleAsync([ProductIdRoute] long productId, [PageQuery] int? page, [RevisionHeader] string? revision, [CatalogService] SampleCatalog catalog, HttpContext context, CancellationToken cancellationToken) => TypedResults.Ok(catalog.Read(productId, page, revision));
}