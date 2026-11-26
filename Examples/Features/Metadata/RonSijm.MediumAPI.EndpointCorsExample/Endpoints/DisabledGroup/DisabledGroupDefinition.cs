using Microsoft.AspNetCore.Cors;

namespace RonSijm.MediumAPI.EndpointCorsExample.Endpoints.DisabledGroup;

[DisableCors]
public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/disabled-group");
}