using System.Diagnostics.CodeAnalysis;
using RonSijm.MediumAPI;

namespace Example.ENDPOINT003.RouteConstraintTypeMismatch;

// ❌ ENDPOINT003 — Route constraint ':int' does not match the handler parameter type 'string'.
//
// The route pattern declares {id:int}, but HandleAsync receives a string.
// The analyzer reports ENDPOINT003: Route parameter 'id' uses constraint 'int', but handler parameter type is 'String'.
internal sealed class GetItemEndpoint : IEndpointAdapter
{
	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "items/{id:int}";

	// ❌ 'string id' does not match ':int' constraint — change to 'int id' to fix ENDPOINT003
	public Task HandleAsync(string id, CancellationToken cancellationToken) => Task.CompletedTask;
}

// ✅ Valid — parameter type matches the route constraint
internal sealed class GetItemEndpointValid : IEndpointAdapter
{
	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "items/{id:int}";

	public Task HandleAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
}
