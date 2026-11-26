using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.DeclarationsExample.Api.Optional;

internal sealed class GenericDeclarationEndpoint : IGetEndpoint<Ok<DeclarationResponse>>
{
    [StringSyntax("Route")]
    public string Pattern => "/generic";

    public Task<Ok<DeclarationResponse>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new DeclarationResponse("generic")));
}