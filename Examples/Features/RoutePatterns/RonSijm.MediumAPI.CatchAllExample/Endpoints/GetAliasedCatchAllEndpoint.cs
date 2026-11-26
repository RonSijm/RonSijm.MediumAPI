using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.CatchAllExample.Endpoints;

internal sealed class GetAliasedCatchAllEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/alias/{**assetPath}";

    public Ok<CatchAllResponse> HandleAsync([FromRoute(Name = "assetPath")] string? path) => TypedResults.Ok(new CatchAllResponse("alias", path));
}