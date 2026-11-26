using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointCorsExample.Endpoints;

[EnableCors(SamplePolicies.Browser)]
public sealed class GetMethodDisabledCorsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-enabled-method-disabled";

    [DisableCors]
    public Ok<CorsReceipt> HandleAsync(CorsProbe probe) => TypedResults.Ok(probe.Record("method-disabled"));
}