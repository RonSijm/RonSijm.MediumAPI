using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.Endpoints.Catalog;

[Tags(SampleMetadata.ParentTag)]
public sealed class CatalogGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/catalog").WithTags(SampleMetadata.MapTag).WithSummary(SampleMetadata.ParentSummary).WithDescription(SampleMetadata.ParentDescription);
        // Native routes registered inside Map receive the final group conventions too.
        group.MapGet("/native", () => TypedResults.Ok(new MetadataResponse("native")));
        return group;
    }
}

internal sealed class GetCatalogDefaultEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/default";

    public Ok<MetadataResponse> HandleAsync() => TypedResults.Ok(new MetadataResponse("catalog"));
}