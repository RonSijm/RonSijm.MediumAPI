using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.MappingReportsExample.Features.Conditional.Child;

public sealed class ChildGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        context.Services.GetRequiredService<MappingProbe>().ChildConditions++;
        return true;
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/child");
}

public sealed class ConditionalChildEndpoint : IGetEndpoint<Ok<MappingValue>>
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public Task<Ok<MappingValue>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new MappingValue("conditional child")));
}

public sealed class ManualNoteEndpoint : IGetEndpoint<Ok<MappingValue>>, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/manual-relative";

    public Task<Ok<MappingValue>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new MappingValue("manual alias")));
}