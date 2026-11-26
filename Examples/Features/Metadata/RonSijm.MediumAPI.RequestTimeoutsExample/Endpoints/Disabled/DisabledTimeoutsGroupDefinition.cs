using Microsoft.AspNetCore.Http.Timeouts;

namespace RonSijm.MediumAPI.RequestTimeoutsExample.Endpoints.Disabled;

[DisableRequestTimeout]
public sealed class DisabledTimeoutsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/group-disabled");
}