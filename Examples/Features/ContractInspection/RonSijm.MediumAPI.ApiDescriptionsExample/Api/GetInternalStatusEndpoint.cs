using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ApiDescriptionsExample.Api;

internal sealed class GetInternalStatusEndpoint : IGetEndpoint<Ok<StatusResponse>>
{
    [StringSyntax("Route")]
    public string Pattern => "/internal-status";

    public Task<Ok<StatusResponse>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new StatusResponse("internal-ready")));

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.ExcludeFromDescription();
}