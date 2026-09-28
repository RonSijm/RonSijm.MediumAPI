using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI;

public interface IEndpoint
{
	HttpVerb Verb { get; }

	[StringSyntax("Route")]
	string Pattern { get; }

	string? AuthorizationPolicy => null;
}
