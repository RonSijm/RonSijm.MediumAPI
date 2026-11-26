using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.CatchAllExample.Endpoints;

internal sealed class GetDoubleNonFileCatchAllEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/constrained/nonfile/double/{**assetPath:nonfile}";

    public Ok<CatchAllResponse> HandleAsync([FromRoute(Name = "assetPath")] string? path) => TypedResults.Ok(new CatchAllResponse("nonfile-double", path));
}