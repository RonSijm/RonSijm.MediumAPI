using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.AuthoringExample.Endpoints.Api;

internal sealed class GetDraftEndpoint : IGetEndpoint
{
    // Intentionally emits ENDPOINT005. In an IDE that has loaded the optional
    // provider, invoke "Add route syntax annotation" on Pattern; the URL stays the same.
    public string Pattern => "/draft";

    public Ok<string> HandleAsync() => TypedResults.Ok("draft");
}