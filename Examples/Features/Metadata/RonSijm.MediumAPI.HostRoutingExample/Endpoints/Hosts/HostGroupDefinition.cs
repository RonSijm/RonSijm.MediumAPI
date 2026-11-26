namespace RonSijm.MediumAPI.HostRoutingExample.Endpoints.Hosts;

[Host(SampleHosts.Group)]
public sealed class HostGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/hosts");
}