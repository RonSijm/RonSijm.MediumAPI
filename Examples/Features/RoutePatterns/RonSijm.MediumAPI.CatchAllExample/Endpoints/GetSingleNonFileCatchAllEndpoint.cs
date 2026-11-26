using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.CatchAllExample.Endpoints;

internal sealed class GetSingleNonFileCatchAllEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/constrained/nonfile/single/{*path:nonfile}";

    public Ok<CatchAllResponse> HandleAsync(string? path) => TypedResults.Ok(new CatchAllResponse("nonfile-single", path));
}