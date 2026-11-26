using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.CatchAllExample.Endpoints;

internal sealed class GetSingleCatchAllEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/single/{*path}";

    public Ok<CatchAllResponse> HandleAsync(string? path) => TypedResults.Ok(new CatchAllResponse("single", path));
}