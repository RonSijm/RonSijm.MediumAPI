using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.Example.Endpoints;

internal sealed class HelloWorldEndpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "hello";

    public string RegistrationGroup => "example-default";

    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken)
        => Task.FromResult(TypedResults.Ok("Hello, World!"));
}