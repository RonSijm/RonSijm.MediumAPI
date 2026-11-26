using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.HostRoutingExample.Endpoints.Hosts;

[Host(SampleHosts.Wildcard)]
public sealed class GetWildcardHostEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/wildcard";

    public Ok<HostResponse> HandleAsync(HostProbe probe) => SampleReplies.Reply(probe, "wildcard");
}