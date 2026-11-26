using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI.CustomBindingExample.Bindings;

namespace RonSijm.MediumAPI.CustomBindingExample.Api;

internal sealed class GetWindowContextEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/context/{segment}";

    // BindAsync reads segment through HttpContext, so there is no separate handler
    // parameter for that token. A nullable parameter accepts the binder's null result.
    public Ok<BindingResponse> HandleAsync(WindowRequest? window) => TypedResults.Ok(new BindingResponse("context", window?.Value ?? "none", window?.ParameterName, window?.RouteValue));
}