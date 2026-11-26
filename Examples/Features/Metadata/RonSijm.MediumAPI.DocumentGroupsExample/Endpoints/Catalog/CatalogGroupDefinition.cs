using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.DocumentGroupsExample.Endpoints.Catalog;

[EndpointGroupName(DocumentGroups.Public)]
[Tags(DocumentTags.Endpoints)]
public sealed class CatalogGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/catalog");
}

internal sealed class GetCatalogValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<DocumentResponse> HandleAsync(DocumentProbe probe) => TypedResults.Ok(probe.Record("catalog"));
}

internal sealed class GetCatalogDetailEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/detail";

    [EndpointGroupName(DocumentGroups.Admin)]
    public Ok<DocumentResponse> HandleAsync(DocumentProbe probe) => TypedResults.Ok(probe.Record("detail"));
}

internal sealed class GetHiddenCatalogValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/hidden";

    [ExcludeFromDescription]
    public Ok<DocumentResponse> HandleAsync(DocumentProbe probe) => TypedResults.Ok(probe.Record("hidden"));
}