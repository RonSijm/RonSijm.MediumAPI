using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.Example;

internal sealed class MetadataEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "metadata/items/{id:int}";

    public string RegistrationGroup => "example-default";

    [EndpointName("GetMetadataItem")]
    [Tags("Metadata")]
    [ProducesResponseType<MetadataItem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> HandleAsync(int id, CancellationToken cancellationToken)
        => Task.FromResult<IResult>(Results.Ok(new MetadataItem(id)));
}

public sealed record MetadataItem(int Id);