using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointCorsExample.Endpoints;

[EnableCors(SamplePolicies.Browser)]
public sealed class GetConfiguredCorsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/configured";

    public Ok<CorsReceipt> HandleAsync(CorsProbe probe) => TypedResults.Ok(probe.Record("configured"));

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.RequireCors(SamplePolicies.Override);
}