using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.CatchAllExample.Endpoints;

internal sealed class GetDoubleIntCatchAllEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/constrained/int/double/{**path:int}";

    public Ok<IntCatchAllResponse> HandleAsync(int path) => TypedResults.Ok(new IntCatchAllResponse("int-double", path));
}