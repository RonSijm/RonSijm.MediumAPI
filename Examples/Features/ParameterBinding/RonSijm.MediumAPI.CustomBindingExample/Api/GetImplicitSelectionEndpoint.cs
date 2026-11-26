using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI.CustomBindingExample.Bindings;

namespace RonSijm.MediumAPI.CustomBindingExample.Api;

internal sealed class GetImplicitSelectionEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/selection/implicit";

    public Ok<BindingResponse> HandleAsync(SelectionKey selected) => TypedResults.Ok(new BindingResponse(selected.Source, selected.Value));
}