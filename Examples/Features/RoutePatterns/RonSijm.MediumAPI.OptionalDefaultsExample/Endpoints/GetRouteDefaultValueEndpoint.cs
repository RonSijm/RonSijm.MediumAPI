using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.OptionalDefaultsExample.Endpoints;

internal sealed class GetRouteDefaultValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/route-default/{value:int=7}";

    public Ok<OptionalValueResponse> HandleAsync([FromRoute] int value) => TypedResults.Ok(new OptionalValueResponse("route-default", value));
}