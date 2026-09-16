using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace RonSijm.MediumAPI.Example.Endpoints;

internal sealed class HelloWorldEndpoint : IEndpointAdapter
{
	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "hello";

	public Task<Ok<string>> HandleAsync(CancellationToken cancellationToken)
		=> Task.FromResult(TypedResults.Ok("Hello, World!"));
}
