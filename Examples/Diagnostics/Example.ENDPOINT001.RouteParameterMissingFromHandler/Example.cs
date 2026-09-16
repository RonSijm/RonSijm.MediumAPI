using System.Diagnostics.CodeAnalysis;
using RonSijm.MediumAPI;

namespace Example.ENDPOINT001.RouteParameterMissingFromHandler;

// ❌ ENDPOINT001 — Route parameter '{id}' is not matched by any HandleAsync parameter.
//
// The route pattern declares {id:int}, but HandleAsync has no 'id' parameter.
// The analyzer reports ENDPOINT001: Route parameter 'id' does not match any handler parameter.
internal sealed class GetItemEndpoint : IEndpointAdapter
{
	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "items/{id:int}";

	// ❌ Missing 'int id' parameter — add it to fix ENDPOINT001
	public Task HandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// ✅ Valid — route parameter matches handler parameter
internal sealed class GetItemEndpointValid : IEndpointAdapter
{
	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "items/{id:int}";

	public Task HandleAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
}
