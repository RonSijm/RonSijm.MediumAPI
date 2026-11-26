using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.Endpoints.Hidden.Child;

public sealed class HiddenChildGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/child");
}

internal sealed class GetHiddenChildStatusEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public Ok<MetadataResponse> HandleAsync() => TypedResults.Ok(new MetadataResponse("hidden-child"));
}