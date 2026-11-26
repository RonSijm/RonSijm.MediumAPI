using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI.CustomBindingExample.Bindings;

namespace RonSijm.MediumAPI.CustomBindingExample.Api.Products;

internal sealed class GetProductDetailsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/details";

    public Ok<BindingResponse> HandleAsync([FromRoute(Name = "productId")] ProductKey key) => TypedResults.Ok(new BindingResponse("route", key.Value.ToString(CultureInfo.InvariantCulture)));
}