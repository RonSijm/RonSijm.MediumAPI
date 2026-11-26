using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.HostRoutingExample.Endpoints.Hosts;

[Host(SampleHosts.Port, SampleHosts.SecurePort, SampleHosts.HttpPort)]
public sealed class GetPortHostEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/port";

    public Ok<HostResponse> HandleAsync(HostProbe probe) => SampleReplies.Reply(probe, "port");
}