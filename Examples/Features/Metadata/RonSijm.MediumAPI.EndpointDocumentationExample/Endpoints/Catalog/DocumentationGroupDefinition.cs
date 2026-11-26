namespace RonSijm.MediumAPI.EndpointDocumentationExample.Endpoints.Catalog;

[Tags(DocumentationTags.Group)]
[EndpointGroupName(DocumentationGroups.Public)]
public sealed class DocumentationGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/documentation");
}