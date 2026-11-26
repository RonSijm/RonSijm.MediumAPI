using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.DocumentGroupsExample.Endpoints.Catalog.Admin;

[EndpointGroupName(DocumentGroups.Admin)]
public sealed class AdminGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/admin");
}

internal sealed class GetAdminValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<DocumentResponse> HandleAsync(DocumentProbe probe) => TypedResults.Ok(probe.Record("admin"));
}