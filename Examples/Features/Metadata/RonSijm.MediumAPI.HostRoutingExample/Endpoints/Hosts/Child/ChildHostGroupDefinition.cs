using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.HostRoutingExample.Endpoints.Hosts.Child;

[Host(SampleHosts.Child)]
public sealed class ChildHostGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/child");
}

public sealed class GetChildHostEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<HostResponse> HandleAsync(HostProbe probe) => SampleReplies.Reply(probe, "child");
}