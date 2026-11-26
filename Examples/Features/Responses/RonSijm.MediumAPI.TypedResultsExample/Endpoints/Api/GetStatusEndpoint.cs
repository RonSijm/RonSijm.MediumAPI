using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.TypedResultsExample.Endpoints.Api;

internal sealed class GetStatusEndpoint : IGetEndpoint<Ok<ServiceStatus>>
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    [EndpointName("TypedGetStatus")]
    public Task<Ok<ServiceStatus>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new ServiceStatus("ready")));
}