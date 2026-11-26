using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI.CustomBindingExample.Bindings;

namespace RonSijm.MediumAPI.CustomBindingExample.Api;

internal sealed class GetLookupCodeEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/codes/{code}";

    public Ok<BindingResponse> HandleAsync([FromRoute] LookupCode code) => TypedResults.Ok(new BindingResponse("route", code.Value));
}