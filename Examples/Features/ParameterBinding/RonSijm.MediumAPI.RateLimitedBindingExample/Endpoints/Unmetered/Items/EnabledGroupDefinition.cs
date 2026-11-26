using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.RateLimitedBindingExample.Endpoints.Unmetered.Items;

public sealed class EnabledGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => RateLimiterEndpointConventionBuilderExtensions.RequireRateLimiting<RouteGroupBuilder>(policyName: SamplePolicies.Named, builder: endpoints.MapGroup("/items/{itemId:int}"));
}

internal sealed class GetUnmeteredItemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<BindingResponse> HandleAsync([FromRoute(Name = "customerId")] long accountId, [FromRoute(Name = "itemId")] int selectedItemId, BindingProbe probe)
    {
        probe.RecordDispatch();
        return TypedResults.Ok(new BindingResponse("unmetered", accountId, selectedItemId));
    }
}