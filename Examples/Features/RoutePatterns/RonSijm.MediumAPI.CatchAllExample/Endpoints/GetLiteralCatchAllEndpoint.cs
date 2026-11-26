using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.CatchAllExample.Endpoints;

internal sealed class GetLiteralCatchAllEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/literal/{{**path}}";

    public Ok<CatchAllResponse> HandleAsync() => TypedResults.Ok(new CatchAllResponse("literal", null));
}