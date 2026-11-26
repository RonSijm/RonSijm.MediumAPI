using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.Endpoints;

[Authorize(SamplePolicies.Readers)]
internal sealed class GetReadAuthorizationEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/read";

    public Ok<AuthorizationReceipt> HandleAsync(HttpContext context, AuthorizationProbe probe) => TypedResults.Ok(probe.Record("read", context.User.Identity?.IsAuthenticated == true));
}