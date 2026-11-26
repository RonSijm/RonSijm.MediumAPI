using Microsoft.AspNetCore.Cors;

namespace RonSijm.MediumAPI.EndpointCorsExample.Endpoints.EnabledGroup;

[EnableCors(SamplePolicies.Browser)]
public sealed class EnabledGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/enabled-group");
}