using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.FeatureExamples.Endpoints.Api.Development;

public sealed class GetDevelopmentValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    [EndpointResponse(StatusCodes.Status200OK, typeof(string))]
    public IResult HandleAsync() => Results.Ok("development");
}