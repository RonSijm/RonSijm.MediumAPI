using Microsoft.AspNetCore.Cors;

namespace RonSijm.MediumAPI.GroupPoliciesExample.Endpoints.Api.Public.LocalOnly;

[DisableCors]
public sealed class LocalOnlyGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/local-only");
}