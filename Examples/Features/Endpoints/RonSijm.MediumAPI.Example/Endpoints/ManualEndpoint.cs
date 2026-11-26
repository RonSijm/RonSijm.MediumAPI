using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.Example.Endpoints;

internal sealed class ManualEndpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "/manual";

    public string RegistrationGroup => "manual";

    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken)
        => Task.FromResult(TypedResults.Ok("manual"));
}