using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.OptionalDefaultsExample.Endpoints;

internal sealed class GetHandlerDefaultValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/handler-default/{value:int?}";

    public Ok<OptionalValueResponse> HandleAsync([FromRoute] int value = 11) => TypedResults.Ok(new OptionalValueResponse("handler-default", value));
}