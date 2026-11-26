using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.EndpointNamesExample.Endpoints.Api;

// The default name is GetProduct; no name attribute or convention is required.
internal sealed class GetProductEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/products/{id:int}";

    public Task<Ok<ProductResponse>> HandleAsync([FromRoute] int id, CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new ProductResponse(id, "Sample notebook")));
}