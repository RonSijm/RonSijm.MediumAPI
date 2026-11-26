using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.Endpoints;

internal sealed class GetAntiforgeryTokenEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/token";

    public Ok<AntiforgeryTokenResponse> HandleAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        return TypedResults.Ok(new AntiforgeryTokenResponse(tokens.RequestToken!, tokens.HeaderName, tokens.FormFieldName));
    }
}