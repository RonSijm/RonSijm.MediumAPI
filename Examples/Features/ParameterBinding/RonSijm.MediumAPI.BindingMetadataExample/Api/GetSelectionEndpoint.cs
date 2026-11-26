using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI.BindingMetadataExample.Bindings;

namespace RonSijm.MediumAPI.BindingMetadataExample.Api;

internal sealed class GetSelectionEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/selection";

    public Ok<SelectionResponse> HandleAsync([SelectionHeader] SelectionKey selected) => TypedResults.Ok(new SelectionResponse(selected.Value, selected.Source));
}