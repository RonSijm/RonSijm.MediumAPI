using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.RouteBindingExample.Endpoints.Api.Items;

internal sealed class GetNarrowCustomerEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/narrow/{id:int}";

    // Intentionally demonstrates ENDPOINT015: the inherited customerId:long
    // token permits values wider than this existing int parameter can bind.
    public Task<Ok<RouteBindingResponse>> HandleAsync([FromRoute] int customerId, [FromRoute] int id, CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new RouteBindingResponse(customerId, id, null)));
}