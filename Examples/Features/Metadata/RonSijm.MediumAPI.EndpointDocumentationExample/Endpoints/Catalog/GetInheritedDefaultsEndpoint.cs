using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.Endpoints.Catalog;

public sealed class GetInheritedDefaultsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inherited";

    public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("inherited"));
}