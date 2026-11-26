using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ContractsExample.Endpoints.Api;

internal sealed class ListItemsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => ContractRoutes.Items;

    [EndpointName("ContractsListItems")]
    [EndpointResponse(200, typeof(ItemResponse[]))]
    public Ok<ItemResponse[]> HandleAsync() => TypedResults.Ok(new[] { new ItemResponse(1, "Sample item") });
}

public sealed record ItemResponse(int Id, string Name);