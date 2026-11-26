using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.Endpoints.Catalog;

[Tags(DocumentationTags.Admin)]
[EndpointGroupName(DocumentationGroups.Admin)]
public sealed class GetConfiguredOverrideEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/configured";

    public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("configured"));
    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithGroupName(DocumentationGroups.Public).WithTags(DocumentationTags.Configured, DocumentationTags.Read);
}