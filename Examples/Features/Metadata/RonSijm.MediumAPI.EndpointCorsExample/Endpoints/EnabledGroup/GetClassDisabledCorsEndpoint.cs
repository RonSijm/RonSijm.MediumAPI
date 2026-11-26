using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointCorsExample.Endpoints.EnabledGroup;

[DisableCors]
public sealed class GetClassDisabledCorsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-disabled";

    public Ok<CorsReceipt> HandleAsync(CorsProbe probe) => TypedResults.Ok(probe.Record("class-disabled"));
}