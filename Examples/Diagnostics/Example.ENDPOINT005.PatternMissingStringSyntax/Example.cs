using System.Diagnostics.CodeAnalysis;
using RonSijm.MediumAPI;

namespace Example.ENDPOINT005.PatternMissingStringSyntax;

// ❌ ENDPOINT005 — Pattern property is missing [StringSyntax("Route")].
//
// Without the attribute, IDE route tooling (syntax highlighting, IntelliSense)
// does not recognise the string as a route pattern.
// The analyzer reports ENDPOINT005: IEndpointAdapter.Pattern property should be decorated with [StringSyntax("Route")].
internal sealed class GetItemEndpoint : IEndpointAdapter
{
	public HttpVerb Verb => HttpVerb.Get;

	// ❌ Missing [StringSyntax("Route")] — add it to fix ENDPOINT005
	public string Pattern => "items/{id:int}";

	public Task HandleAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
}

// ✅ Valid — attribute is present
internal sealed class GetItemEndpointValid : IEndpointAdapter
{
	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "items/{id:int}";

	public Task HandleAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
}
