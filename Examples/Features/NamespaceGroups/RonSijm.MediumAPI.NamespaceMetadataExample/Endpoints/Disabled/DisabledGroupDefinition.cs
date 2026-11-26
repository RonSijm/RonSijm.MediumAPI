using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.Endpoints.Disabled;

[Tags(SampleMetadata.DisabledTag)]
[ExcludeFromDescription]
public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        context.Services.GetRequiredService<MappingProbe>().DisabledConditions++;
        return false;
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.ServiceProvider.GetRequiredService<MappingProbe>().DisabledMaps++;
        throw new InvalidOperationException("The disabled sample namespace must not map.");
    }
}

internal sealed class GetDisabledStatusEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public Ok<MetadataResponse> HandleAsync() => TypedResults.Ok(new MetadataResponse("disabled"));
}