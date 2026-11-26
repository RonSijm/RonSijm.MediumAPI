using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.Example.Endpoints;

internal sealed class GroupedSecondEndpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public string RegistrationGroup => "second";

    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken)
        => Task.FromResult(TypedResults.Ok("second"));

    public static void Configure(RouteHandlerBuilder endpoint)
        => endpoint.WithName("GetSecondValue").WithTags("Groups");
}