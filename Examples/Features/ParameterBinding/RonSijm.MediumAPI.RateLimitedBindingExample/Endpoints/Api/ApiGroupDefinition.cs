using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.RateLimitedBindingExample.Endpoints.Api;

public sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api/{customerId:long}").RequireRateLimiting(SamplePolicies.CreateNamedPolicyName(endpoints.ServiceProvider));
}

internal sealed class GetCustomerValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<BindingResponse> HandleAsync([FromRoute(Name = "customerId")] long accountId, BindingProbe probe)
    {
        probe.RecordDispatch();
        return TypedResults.Ok(new BindingResponse("customer", accountId, null));
    }
}