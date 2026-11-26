using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.Endpoints.DeclaredOnly;

[Tags(SampleMetadata.BaseTag)]
[ExcludeFromDescription]
public abstract class MetadataBase { }

public sealed class DeclaredOnlyGroupDefinition : MetadataBase, INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/declared-only");
}

internal sealed class GetDeclaredStatusEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    public Ok<MetadataResponse> HandleAsync() => TypedResults.Ok(new MetadataResponse("declared-only"));
}