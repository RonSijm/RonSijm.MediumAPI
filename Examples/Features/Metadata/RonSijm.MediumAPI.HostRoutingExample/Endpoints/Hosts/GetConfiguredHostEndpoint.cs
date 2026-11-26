using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.HostRoutingExample.Endpoints.Hosts;

[Host(SampleHosts.Api)]
public sealed class GetConfiguredHostEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/configured";

    [Host(SampleHosts.Method)]
    public Ok<HostResponse> HandleAsync(HostProbe probe) => SampleReplies.Reply(probe, "configured");

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.RequireHost(SampleHosts.Configured);
}