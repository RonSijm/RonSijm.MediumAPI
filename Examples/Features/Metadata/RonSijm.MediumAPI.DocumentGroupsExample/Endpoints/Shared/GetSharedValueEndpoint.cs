using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.DocumentGroupsExample.Endpoints.Shared;

internal sealed class GetSharedValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/shared";

    [Tags(DocumentTags.Endpoints)]
    public Ok<DocumentResponse> HandleAsync(DocumentProbe probe) => TypedResults.Ok(probe.Record("shared"));
}