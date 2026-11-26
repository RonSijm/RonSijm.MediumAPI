using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointNamesExample.Endpoints.Api;

internal sealed class CatalogView : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "/catalog-view";

    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok("no suffix"));
}

internal sealed class EndpointDiagnosticsEndpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "/diagnostics";

    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok("middle occurrence retained"));
}

internal sealed class GetEndpointEndpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "/repeated-suffix";

    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok("one suffix removed"));
}

internal sealed class Lowercaseendpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "/case-sensitive";

    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok("case retained"));
}

internal sealed class Endpoint : IGetEndpoint<Ok<string>>
{
    [StringSyntax("Route")]
    public string Pattern => "/exact-endpoint";

    public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok("nonempty name"));
}