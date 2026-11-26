using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.HostRoutingExample.Endpoints.Open;

public sealed class GetOpenHostEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/open";

    public Ok<HostResponse> HandleAsync(HostProbe probe) => SampleReplies.Reply(probe, "open");
}