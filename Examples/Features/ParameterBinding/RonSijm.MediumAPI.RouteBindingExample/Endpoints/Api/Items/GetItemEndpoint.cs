using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.RouteBindingExample.Endpoints.Api.Items;

internal sealed class GetItemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/{id:int}";

    // The explicit route alias binds a token declared by the parent group.
    public Task<Ok<RouteBindingResponse>> HandleAsync([FromRoute(Name = "customerId")] long accountId, [FromRoute] int id, CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new RouteBindingResponse(accountId, id, null)));
}