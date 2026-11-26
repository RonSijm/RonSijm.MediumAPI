using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.Endpoints;

[Authorize(SamplePolicies.Readers)]
internal sealed class GetPublicMethodAuthorizationEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/public-method";

    [AllowAnonymous]
    public Ok<AuthorizationReceipt> HandleAsync(HttpContext context, AuthorizationProbe probe) => TypedResults.Ok(probe.Record("public-method", context.User.Identity?.IsAuthenticated == true));
}