using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.Endpoints.Catalog;

[Tags(DocumentationTags.Catalog, DocumentationTags.Read)]
[EndpointGroupName(DocumentationGroups.Public)]
public sealed class GetMethodOverrideEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method";

    [Tags(DocumentationTags.Method)]
    [EndpointGroupName(DocumentationGroups.Admin)]
    public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("method"));
}