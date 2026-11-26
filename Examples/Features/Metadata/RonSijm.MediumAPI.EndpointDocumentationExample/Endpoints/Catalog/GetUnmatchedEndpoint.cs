using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.Endpoints.Catalog;

[Tags(DocumentationTags.Unmatched)]
[EndpointGroupName(DocumentationGroups.Unmatched)]
public sealed class GetUnmatchedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/unmatched";

    public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("unmatched"));
}