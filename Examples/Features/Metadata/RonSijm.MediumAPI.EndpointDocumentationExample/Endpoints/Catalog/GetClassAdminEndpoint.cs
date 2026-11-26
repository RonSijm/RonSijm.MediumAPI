using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.Endpoints.Catalog;

[Tags(DocumentationTags.Admin)]
[EndpointGroupName(DocumentationGroups.Admin)]
public sealed class GetClassAdminEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/admin";

    public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("admin"));
}