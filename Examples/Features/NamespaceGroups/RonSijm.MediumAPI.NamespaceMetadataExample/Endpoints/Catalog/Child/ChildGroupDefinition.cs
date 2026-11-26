using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.Endpoints.Catalog.Child;

[Tags(SampleMetadata.ChildTag)]
public sealed class ChildGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/child").WithSummary(SampleMetadata.ChildSummary).WithDescription(SampleMetadata.ChildDescription);
}

internal sealed class GetCatalogChildEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/default";

    public Ok<MetadataResponse> HandleAsync() => TypedResults.Ok(new MetadataResponse("child"));
}

internal sealed class GetCatalogDetailEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/detail";

    [Tags(SampleMetadata.DetailTag)]
    [EndpointSummary(SampleMetadata.DetailSummary)]
    [EndpointDescription(SampleMetadata.DetailDescription)]
    public Ok<MetadataResponse> HandleAsync() => TypedResults.Ok(new MetadataResponse("detail"));
}