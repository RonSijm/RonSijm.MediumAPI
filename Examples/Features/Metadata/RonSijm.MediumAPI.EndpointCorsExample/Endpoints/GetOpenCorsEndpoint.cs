using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointCorsExample.Endpoints;

public sealed class GetOpenCorsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/open";

    public Ok<CorsReceipt> HandleAsync(CorsProbe probe) => TypedResults.Ok(probe.Record("open"));
}