using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.StructBindingExample.Endpoints;

internal sealed class GetLocalStructItemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/items/{itemId:long}";

    [EndpointFilter<AggregateArgumentFilter>]
    public Ok<AggregateResponse> HandleAsync([AsParameters] LocalItemRequest request, HttpContext context)
    {
        context.RequestServices.GetRequiredService<AggregateProbe>().RecordHandler();
        return TypedResults.Ok(new AggregateResponse("local", request.SelectedItem, null, request.Page, request.Label, request.Service.Name, AggregateArgumentFilter.MatchesBoundValue(context, request), AggregateArgumentFilter.MatchesService(context, request.Service), AggregateArgumentFilter.GetArgumentCount(context), false));
    }
}