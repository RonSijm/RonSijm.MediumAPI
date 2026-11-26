using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using RonSijm.MediumAPI;

namespace MediumApi.TemplateNamespace;

internal sealed class MediumApiEndpoint : I__MediumApiVerb__Endpoint
{
	[StringSyntax("Route")]
	public string Pattern => "__MediumApiRoute__";

	public IResult HandleAsync() => Results.Ok();
}
