using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.DeclarationsExample.Shared;

// Abstract helpers are not independently registered. A concrete endpoint may
// inherit its single public handler and readable metadata from this source.
internal abstract class DeclarationEndpointBase : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inherited";

    public Task<Ok<DeclarationResponse>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new DeclarationResponse("inherited")));
}