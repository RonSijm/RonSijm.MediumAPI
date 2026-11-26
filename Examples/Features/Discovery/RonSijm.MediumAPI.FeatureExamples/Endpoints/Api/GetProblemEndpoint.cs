using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.FeatureExamples.Endpoints.Api;

public sealed class GetProblemEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/problem";

    [EndpointName("FeatureProblem")]
    [EndpointResponse(StatusCodes.Status400BadRequest, typeof(ProblemDetails), "application/problem+json")]
    public IResult HandleAsync() => Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Example problem");
}