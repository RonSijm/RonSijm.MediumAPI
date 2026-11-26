using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.DeclarationsExample.Api;

internal sealed partial class PartialDeclarationEndpoint
{
    public Task<Ok<DeclarationResponse>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new DeclarationResponse("partial")));

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithSummary("One endpoint declared across source files").WithTags("declarations");
}