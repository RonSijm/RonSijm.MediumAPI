using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.AsParametersExample.Endpoints;

internal sealed class GetLocalItemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/items/{itemId:long}";

    [EndpointFilter<AggregateArgumentFilter>]
    public Ok<AggregateResponse> HandleAsync([AsParameters] LocalItemRequest request, HttpContext context)
    {
        context.RequestServices.GetRequiredService<AggregateProbe>().RecordHandler();
        return TypedResults.Ok(new AggregateResponse("local", request.SelectedItem, null, request.Page, request.Label, request.Service.Name, AggregateArgumentFilter.IsOriginalArgument(context, request), AggregateArgumentFilter.GetArgumentCount(context)));
    }
}