using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.RateLimitedBindingExample.Endpoints.Api.Items;

public sealed class ItemsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/items/{itemId:int}").RequireRateLimiting(SamplePolicies.CreateTypedPolicy(endpoints.ServiceProvider));
}

internal sealed class GetLimitedItemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<BindingResponse> HandleAsync([FromRoute(Name = "customerId")] long accountId, [FromRoute(Name = "itemId")] int selectedItemId, BindingProbe probe)
    {
        probe.RecordDispatch();
        return TypedResults.Ok(new BindingResponse("item", accountId, selectedItemId));
    }
}