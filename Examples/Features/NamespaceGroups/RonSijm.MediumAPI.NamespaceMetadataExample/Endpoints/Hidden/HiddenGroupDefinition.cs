using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.Endpoints.Hidden;

[Tags(SampleMetadata.HiddenTag)]
[ExcludeFromDescription]
public sealed class HiddenGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/hidden");
}

internal sealed class GetHiddenStatusEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public Ok<MetadataResponse> HandleAsync() => TypedResults.Ok(new MetadataResponse("hidden"));
}