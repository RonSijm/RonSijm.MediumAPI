using System.Diagnostics.CodeAnalysis;
using RonSijm.MediumAPI;

namespace Example.ENDPOINT002.HandlerParameterMissingFromRoute;

// ❌ ENDPOINT002 — Handler parameter 'id' is not present in the route pattern.
//
// HandleAsync declares 'int id', but the route "items" has no {id} segment.
// The analyzer reports ENDPOINT002: Handler parameter 'id' is not present in the route.
internal sealed class GetItemEndpoint : IEndpointAdapter
{
	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "items";   // ❌ Missing {id:int} — add it to fix ENDPOINT002

	public Task HandleAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
}

// ✅ Valid — handler parameter matches a route segment
internal sealed class GetItemEndpointValid : IEndpointAdapter
{
	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "items/{id:int}";

	public Task HandleAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
}
