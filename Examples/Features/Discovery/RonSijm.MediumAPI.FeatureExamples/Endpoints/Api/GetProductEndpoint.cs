using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.FeatureExamples.Endpoints.Api;

public sealed class GetProductEndpoint : IGetEndpoint
{
    public GetProductEndpoint(FeatureMappingProbe probe)
    {
        probe.EndpointActivations++;
    }

    [StringSyntax("Route")]
    public string Pattern => "/products/{productId:int}";

    [EndpointName("FeatureProduct")]
    [EndpointResponse(StatusCodes.Status200OK, typeof(ProductResponse))]
    [EndpointResponse(StatusCodes.Status404NotFound)]
    public IResult HandleAsync([FromRoute] int productId)
    {
        return productId > 0 ? Results.Ok(new ProductResponse(productId, "Sample product")) : Results.NotFound();
    }
}

public sealed record ProductResponse(int Id, string Name);