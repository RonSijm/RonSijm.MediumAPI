using Microsoft.AspNetCore.Authorization;

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.Endpoints.Protected;

[Authorize(SamplePolicies.GroupAccess)]
public sealed class ProtectedGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/protected");
}