using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.CatchAllExample.Endpoints;

internal sealed class GetRequiredCatchAllEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/required/{**path}";

    public Ok<CatchAllResponse> HandleAsync(string path) => TypedResults.Ok(new CatchAllResponse("required", path));
}