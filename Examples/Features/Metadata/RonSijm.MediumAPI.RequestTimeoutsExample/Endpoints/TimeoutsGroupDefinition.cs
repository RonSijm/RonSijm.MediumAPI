using Microsoft.AspNetCore.Http.Timeouts;

namespace RonSijm.MediumAPI.RequestTimeoutsExample.Endpoints;

[RequestTimeout(TimeoutPolicies.Group)]
public sealed class TimeoutsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/timeouts");
}