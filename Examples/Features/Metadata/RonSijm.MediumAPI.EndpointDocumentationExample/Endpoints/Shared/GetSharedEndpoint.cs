using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.Endpoints.Shared;

[Tags(DocumentationTags.Shared, DocumentationTags.Read)]
public sealed class GetSharedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/shared";

    public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("shared"));
}