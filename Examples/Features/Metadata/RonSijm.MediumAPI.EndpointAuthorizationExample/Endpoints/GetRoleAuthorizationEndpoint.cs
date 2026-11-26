using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.Endpoints;

[Authorize(Roles = "Reader,Administrator", AuthenticationSchemes = SamplePolicies.SecondaryScheme)]
internal sealed class GetRoleAuthorizationEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/roles";

    public Ok<AuthorizationReceipt> HandleAsync(HttpContext context, AuthorizationProbe probe) => TypedResults.Ok(probe.Record("roles", context.User.Identity?.IsAuthenticated == true));
}