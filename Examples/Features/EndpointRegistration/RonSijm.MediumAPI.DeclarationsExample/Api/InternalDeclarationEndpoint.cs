using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.DeclarationsExample.Api;

internal sealed class InternalDeclarationEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/internal";

    public Ok<DeclarationResponse> HandleAsync() => TypedResults.Ok(new DeclarationResponse("internal"));
}