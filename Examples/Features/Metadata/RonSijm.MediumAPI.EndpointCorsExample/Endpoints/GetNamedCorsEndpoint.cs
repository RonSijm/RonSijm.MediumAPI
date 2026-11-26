using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointCorsExample.Endpoints;

[EnableCors(PolicyName = SamplePolicies.Browser)]
public sealed class GetNamedCorsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/named";

    public Ok<CorsReceipt> HandleAsync(CorsProbe probe) => TypedResults.Ok(probe.Record("named"));
}