using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.CatchAllExample.Endpoints;

internal sealed class GetDoubleCatchAllEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/double/{**path}";

    public Ok<CatchAllResponse> HandleAsync(string? path) => TypedResults.Ok(new CatchAllResponse("double", path));
}