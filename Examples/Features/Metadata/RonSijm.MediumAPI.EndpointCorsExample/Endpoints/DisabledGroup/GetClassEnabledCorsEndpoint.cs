using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointCorsExample.Endpoints.DisabledGroup;

[EnableCors(SamplePolicies.Browser)]
public sealed class GetClassEnabledCorsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-enabled";

    public Ok<CorsReceipt> HandleAsync(CorsProbe probe) => TypedResults.Ok(probe.Record("class-enabled"));
}