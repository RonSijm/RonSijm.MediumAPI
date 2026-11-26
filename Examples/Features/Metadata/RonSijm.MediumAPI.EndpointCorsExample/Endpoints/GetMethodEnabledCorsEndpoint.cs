using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointCorsExample.Endpoints;

[DisableCors]
public sealed class GetMethodEnabledCorsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-disabled-method-enabled";

    [EnableCors(SamplePolicies.Browser)]
    public Ok<CorsReceipt> HandleAsync(CorsProbe probe) => TypedResults.Ok(probe.Record("method-enabled"));
}