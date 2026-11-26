using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI.CustomBindingExample.Bindings;

namespace RonSijm.MediumAPI.CustomBindingExample.Api;

internal sealed class GetExplicitSelectionEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/selection";

    public Ok<BindingResponse> HandleAsync([FromQuery(Name = "selection")] SelectionKey selected) => TypedResults.Ok(new BindingResponse(selected.Source, selected.Value));
}