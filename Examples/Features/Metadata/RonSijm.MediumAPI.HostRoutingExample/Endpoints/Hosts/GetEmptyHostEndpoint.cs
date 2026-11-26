using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.HostRoutingExample.Endpoints.Hosts;

[Host]
public sealed class GetEmptyHostEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/empty";

    public Ok<HostResponse> HandleAsync(HostProbe probe) => SampleReplies.Reply(probe, "empty");
}