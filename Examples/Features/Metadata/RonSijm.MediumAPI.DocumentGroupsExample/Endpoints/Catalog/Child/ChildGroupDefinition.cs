using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.DocumentGroupsExample.Endpoints.Catalog.Child;

public sealed class ChildGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/child");
}

internal sealed class GetChildValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<DocumentResponse> HandleAsync(DocumentProbe probe) => TypedResults.Ok(probe.Record("child"));
}