using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.Endpoints;

internal sealed class GetOpenAuthorizationEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/open";

    public Ok<AuthorizationReceipt> HandleAsync(HttpContext context, AuthorizationProbe probe) => TypedResults.Ok(probe.Record("open", context.User.Identity?.IsAuthenticated == true));
}