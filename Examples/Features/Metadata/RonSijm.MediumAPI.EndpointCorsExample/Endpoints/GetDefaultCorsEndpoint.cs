using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointCorsExample.Endpoints;

[EnableCors]
public sealed class GetDefaultCorsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/default";

    public Ok<CorsReceipt> HandleAsync(CorsProbe probe) => TypedResults.Ok(probe.Record("default"));
}