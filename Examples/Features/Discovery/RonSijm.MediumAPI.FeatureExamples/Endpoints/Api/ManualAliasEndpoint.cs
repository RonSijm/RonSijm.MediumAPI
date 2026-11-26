using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.FeatureExamples.Endpoints.Api;

public sealed class ManualAliasEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/manual";

    [EndpointResponse(StatusCodes.Status200OK, typeof(string))]
    public IResult HandleAsync() => Results.Ok("manual");
}