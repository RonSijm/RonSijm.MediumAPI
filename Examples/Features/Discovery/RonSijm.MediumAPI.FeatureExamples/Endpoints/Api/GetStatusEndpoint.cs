using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.FeatureExamples.Endpoints.Api;

public sealed class GetStatusEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/status";

    [EndpointName("FeatureStatus")]
    [EndpointResponse(StatusCodes.Status204NoContent)]
    [EndpointResponse(StatusCodes.Status400BadRequest)]
    public IResult HandleAsync() => Results.NoContent();
}