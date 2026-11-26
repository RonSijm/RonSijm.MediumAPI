using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;

namespace RonSijm.MediumAPI.GroupPoliciesExample.Endpoints.Api;

[Authorize(Policy = ExamplePolicies.Readers)]
[EnableCors(ExamplePolicies.Browser)]
public sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api").WithTags("Group policies");
        // Native routes inherit the same final group conventions as generated endpoints.
        group.MapGet("/native-status", () => Results.Ok("native protected"));
        return group;
    }
}