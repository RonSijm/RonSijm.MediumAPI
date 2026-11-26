using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ContractsExample.Endpoints.Api;

internal sealed class GetItemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => ContractRoutes.Item;

    [EndpointName("ContractsGetItem")]
    [EndpointResponse(200, typeof(ItemResponse))]
    [EndpointResponse(404)]
    public IResult HandleAsync([FromRoute(Name = "id")] int itemId) => itemId < 1 ? Results.NotFound() : Results.Ok(new ItemResponse(itemId, "Sample item"));
}