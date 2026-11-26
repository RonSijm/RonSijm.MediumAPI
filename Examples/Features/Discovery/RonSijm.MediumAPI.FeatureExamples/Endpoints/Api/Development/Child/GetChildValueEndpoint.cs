using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.FeatureExamples.Endpoints.Api.Development.Child;

public sealed class GetChildValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    [EndpointResponse(StatusCodes.Status200OK, typeof(string))]
    public IResult HandleAsync() => Results.Ok("child");
}