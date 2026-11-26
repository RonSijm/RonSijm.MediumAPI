using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.MappingReportsExample.RootEndpoints;

public sealed class RootPingEndpoint : IGetEndpoint<Ok<MappingValue>>
{
    public RootPingEndpoint(MappingProbe probe)
    {
        probe.EndpointActivations++;
    }

    [StringSyntax("Route")]
    public string Pattern => "/ping";

    public Task<Ok<MappingValue>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new MappingValue("root")));

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.Add(builder => builder.ApplicationServices.GetRequiredService<MappingProbe>().MetadataBuilds++);
}