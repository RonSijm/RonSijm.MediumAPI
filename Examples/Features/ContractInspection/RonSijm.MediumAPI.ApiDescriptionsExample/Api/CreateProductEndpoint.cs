using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ApiDescriptionsExample.Api;

internal sealed class CreateProductEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/products";

    public Created<ProductCreatedResponse> HandleAsync([FromBody] ProductCreateRequest create, [FromHeader(Name = "X-Sample-Request")] string? requestId, CancellationToken cancellationToken) => TypedResults.Created("/api/products/7", new ProductCreatedResponse(7, create.Name, requestId));
}