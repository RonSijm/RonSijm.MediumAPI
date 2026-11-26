using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.ScaffoldingExample.Endpoints.Api;

internal sealed class GetProductsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/products";

    public IResult HandleAsync() => Results.Ok();
}