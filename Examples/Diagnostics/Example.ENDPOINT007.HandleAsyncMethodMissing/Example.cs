using System.Diagnostics.CodeAnalysis;
using RonSijm.MediumAPI;

namespace Example.ENDPOINT007.HandleAsyncMethodMissing;

// ❌ ENDPOINT007 — IEndpointAdapter implementation is missing a HandleAsync method.
//
// The source generator uses reflection to locate HandleAsync at runtime.
// Without it, MapMediumApiEndpoints() will throw a NullReferenceException.
// The analyzer reports ENDPOINT007: Endpoint 'GetItemEndpoint' must declare a public HandleAsync method.
internal sealed class GetItemEndpoint : IEndpointAdapter
{
	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "items/{id:int}";

	// ❌ HandleAsync is missing — add it to fix ENDPOINT007
}

// ✅ Valid — HandleAsync is declared
internal sealed class GetItemEndpointValid : IEndpointAdapter
{
	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "items/{id:int}";

	public Task HandleAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
}
