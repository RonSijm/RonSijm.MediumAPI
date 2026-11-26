using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.CatchAllExample.Endpoints;

internal sealed class GetSingleIntCatchAllEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/constrained/int/single/{*path:int}";

    public Ok<IntCatchAllResponse> HandleAsync(int path) => TypedResults.Ok(new IntCatchAllResponse("int-single", path));
}