using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.Endpoints.Catalog;

[Tags(DocumentationTags.Hidden)]
[EndpointGroupName(DocumentationGroups.Admin)]
[ExcludeFromDescription]
public sealed class GetHiddenEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/hidden";

    public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("hidden"));
}