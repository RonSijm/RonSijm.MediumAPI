using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.MappingReportsExample.Features.Enabled;

public sealed class EnabledGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        context.Services.GetRequiredService<MappingProbe>().EnabledConditions++;
        return true;
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api/enabled");
}

public sealed class EnabledStatusEndpoint : IGetEndpoint<Ok<MappingValue>>
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public Task<Ok<MappingValue>> HandleAsync(CancellationToken cancellationToken) => Task.FromResult(TypedResults.Ok(new MappingValue("enabled")));
}