using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.RouteBindingExample.Endpoints.Api.Items;

internal sealed class GetHostTenantEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/host-context";

    // Intentionally demonstrates ENDPOINT014. Namespace declarations contain no
    // tenant token, but Program supplies it in the real outer host prefix.
    public Task<Ok<RouteBindingResponse>> HandleAsync([FromRoute] long customerId, [FromRoute(Name = "tenant")] string tenantKey, CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new RouteBindingResponse(customerId, null, tenantKey)));
}