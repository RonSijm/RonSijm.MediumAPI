using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.HostRoutingExample.Endpoints.Hosts;

public sealed class GetInheritedHostEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inherited";

    public Ok<HostResponse> HandleAsync(HostProbe probe) => SampleReplies.Reply(probe, "inherited");
}