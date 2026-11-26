using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.Endpoints;

[Authorize(SamplePolicies.Readers)]
internal sealed class GetPropertyAuthorizationEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/property";

    public string? AuthorizationPolicy => SamplePolicies.Writers;
    public Ok<AuthorizationReceipt> HandleAsync(HttpContext context, AuthorizationProbe probe) => TypedResults.Ok(probe.Record("property", context.User.Identity?.IsAuthenticated == true));
    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.RequireAuthorization(SamplePolicies.Audit);
}