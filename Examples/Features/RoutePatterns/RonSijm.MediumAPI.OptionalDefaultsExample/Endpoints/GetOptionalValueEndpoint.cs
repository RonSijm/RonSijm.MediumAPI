using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.OptionalDefaultsExample.Endpoints;

internal sealed class GetOptionalValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/optional/{value:int?}";

    public Ok<OptionalValueResponse> HandleAsync([FromRoute] int? value) => TypedResults.Ok(new OptionalValueResponse("optional", value));
}