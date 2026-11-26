using Microsoft.AspNetCore.Authorization;

namespace RonSijm.MediumAPI.GroupPoliciesExample.Endpoints.Api.Public;

[AllowAnonymous]
public sealed class PublicGroupDefinition : INamespaceEndpointGroup
{
    // Anonymous access is deliberate; this subtree still inherits the parent's CORS policy.
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/public");
}