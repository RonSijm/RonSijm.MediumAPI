using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointNamesExample.Endpoints.Api.Unnamed;

internal sealed class UnnamedGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/unnamed").WithoutDefaultEndpointName();
}

internal sealed class UnnamedGroupEndpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok("unnamed group"));
}

internal sealed class ExplicitInsideUnnamedGroupEndpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "/explicit";

    [EndpointName("ExplicitInsideUnnamedGroup")]
    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok("explicit survives suppression"));
}