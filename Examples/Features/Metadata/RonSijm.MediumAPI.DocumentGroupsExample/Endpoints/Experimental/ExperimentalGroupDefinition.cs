using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.DocumentGroupsExample.Endpoints.Experimental;

[EndpointGroupName(DocumentGroups.Unmatched)]
[Tags(DocumentTags.Endpoints)]
public sealed class ExperimentalGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/experimental");
}

internal sealed class GetExperimentalValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public Ok<DocumentResponse> HandleAsync(DocumentProbe probe) => TypedResults.Ok(probe.Record("experimental"));
}