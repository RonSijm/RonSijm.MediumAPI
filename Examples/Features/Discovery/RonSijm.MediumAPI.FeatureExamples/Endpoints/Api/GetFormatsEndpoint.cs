using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.FeatureExamples.Endpoints.Api;

public sealed class GetFormatsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/formats";

    [EndpointName("FeatureFormats")]
    [EndpointResponse(StatusCodes.Status200OK, typeof(string), "text/plain", "application/json")]
    public IResult HandleAsync() => Results.Text("plain", "text/plain");
}