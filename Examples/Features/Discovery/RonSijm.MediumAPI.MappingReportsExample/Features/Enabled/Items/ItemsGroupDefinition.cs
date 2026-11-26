using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.MappingReportsExample.Features.Enabled.Items;

public sealed class ItemsGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        context.Services.GetRequiredService<MappingProbe>().NestedConditions++;
        return true;
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/items");
}

public sealed class GetEnabledItemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/{id:int}";

    public Task<Ok<MappingValue>> HandleAsync([FromRoute] int id, CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new MappingValue("item-" + id)));
}