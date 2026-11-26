using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.AuthoringExample.Endpoints.Api;

internal sealed class GetReadyEndpoint : IGetEndpoint
{
    [global::System.Diagnostics.CodeAnalysis.StringSyntax("Route")]
    public string Pattern => "/ready";

    public Ok<string> HandleAsync() => TypedResults.Ok("ready");
}