using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ApiDescriptionsExample.Api;

internal sealed class GetProductEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/products/{id:long}";

    public Ok<ProductDetailsResponse> HandleAsync([FromRoute(Name = "id")] long productId, [FromQuery(Name = "search")] string? search, [FromHeader(Name = "X-Sample-Revision")] string? revision, [FromServices] SampleCatalog catalog, HttpContext context, CancellationToken cancellationToken) => TypedResults.Ok(catalog.Read(productId, search, revision));
}