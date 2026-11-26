using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.HostRoutingExample.Endpoints.Hosts;

[Host(SampleHosts.Api)]
public sealed class GetMethodHostEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method";

    [Host(SampleHosts.Method)]
    public Ok<HostResponse> HandleAsync(HostProbe probe) => SampleReplies.Reply(probe, "method");
}