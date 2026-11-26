using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.BindingMetadataExample.Api;

internal sealed class GetStatusEndpoint : IGetEndpoint<Ok<StatusResponse>>
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public Task<Ok<StatusResponse>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new StatusResponse("ready")));
}