using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.HostRoutingExample.Endpoints.Hosts;

[Host(SampleHosts.Api, SampleHosts.Alternate)]
public sealed class GetClassHostsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class";

    public Ok<HostResponse> HandleAsync(HostProbe probe) => SampleReplies.Reply(probe, "class");
}