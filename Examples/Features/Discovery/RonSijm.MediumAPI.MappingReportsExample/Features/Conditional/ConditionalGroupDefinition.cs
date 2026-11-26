using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.MappingReportsExample.Features.Conditional;

public sealed class ConditionalGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        var probe = context.Services.GetRequiredService<MappingProbe>();
        probe.ParentConditions++;
        return probe.ConditionalEnabled;
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api/conditional");
}

public sealed class ConditionalStatusEndpoint : IGetEndpoint<Ok<MappingValue>>
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public Task<Ok<MappingValue>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new MappingValue("conditional")));
}