using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ContractsExample.Endpoints.Api;

internal sealed class GetStatusEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => ContractRoutes.Status;

    [EndpointName("ContractsGetStatus")]
    [EndpointResponse(200, typeof(StatusResponse))]
    public Ok<StatusResponse> HandleAsync() => TypedResults.Ok(new StatusResponse("sample"));
}

public sealed record StatusResponse(string Status);