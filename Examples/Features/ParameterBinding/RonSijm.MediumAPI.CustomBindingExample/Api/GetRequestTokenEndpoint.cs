using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI.CustomBindingExample.Bindings;

namespace RonSijm.MediumAPI.CustomBindingExample.Api;

internal sealed class GetRequestTokenEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/request-token";

    public Ok<BindingResponse> HandleAsync(RequestToken token) => TypedResults.Ok(new BindingResponse("context", token.Value, token.ParameterName));
}