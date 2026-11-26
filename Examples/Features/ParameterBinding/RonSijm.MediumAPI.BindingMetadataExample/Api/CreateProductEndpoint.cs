using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI.BindingMetadataExample.Bindings;

namespace RonSijm.MediumAPI.BindingMetadataExample.Api;

internal sealed class CreateProductEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/products";

    public Created<ProductCreatedResponse> HandleAsync([JsonBody] ProductCreateRequest create, [RevisionHeader] string? revision, CancellationToken cancellationToken) => TypedResults.Created("/api/products/7", new ProductCreatedResponse(7, create.Name, revision));
}