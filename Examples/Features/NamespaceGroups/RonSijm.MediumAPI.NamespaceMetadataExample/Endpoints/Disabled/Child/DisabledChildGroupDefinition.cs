using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.Endpoints.Disabled.Child;

public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        context.Services.GetRequiredService<MappingProbe>().DisabledChildConditions++;
        return true;
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.ServiceProvider.GetRequiredService<MappingProbe>().DisabledChildMaps++;
        throw new InvalidOperationException("A descendant of the disabled sample namespace must not map.");
    }
}

internal sealed class GetDisabledChildStatusEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public Ok<MetadataResponse> HandleAsync() => TypedResults.Ok(new MetadataResponse("disabled-child"));
}