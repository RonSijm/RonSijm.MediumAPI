using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI;

public interface IEndpointAdapter
{
	HttpVerb Verb { get; }

	[StringSyntax("Route")]
	string Pattern { get; }

	string? AuthorizationPolicy => null;
}
