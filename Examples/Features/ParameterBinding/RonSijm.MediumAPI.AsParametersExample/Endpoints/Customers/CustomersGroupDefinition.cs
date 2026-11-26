using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.AsParametersExample.Endpoints.Customers;

public sealed class CustomersGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/customers/{customerId:long}");
}

internal sealed class GetWorkspaceItemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/items/{itemId:long}";

    [EndpointFilter<AggregateArgumentFilter>]
    public Ok<AggregateResponse> HandleAsync([AsParameters] WorkspaceItemRequest request, HttpContext context)
    {
        context.RequestServices.GetRequiredService<AggregateProbe>().RecordHandler();
        return TypedResults.Ok(new AggregateResponse("workspace", request.SelectedItem, request.AccountId, request.Page, request.Label, request.Service.Name, AggregateArgumentFilter.IsOriginalArgument(context, request), AggregateArgumentFilter.GetArgumentCount(context)));
    }
}