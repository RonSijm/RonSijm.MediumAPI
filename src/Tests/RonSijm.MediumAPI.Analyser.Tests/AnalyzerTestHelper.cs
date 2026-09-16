using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace RonSijm.MediumAPI.Analyser.Tests;

internal static class AnalyzerTestHelper
{
	/// <summary>
	/// Stub definitions of the RonSijm.MediumAPI types.
	/// The analyzer identifies endpoints by interface name, not assembly identity,
	/// so these stubs are sufficient for all diagnostic tests.
	/// </summary>
	private const string StubTypes = """
		using System.Diagnostics.CodeAnalysis;
		namespace RonSijm.MediumAPI
		{
		    public enum HttpVerb { Get, Post, Put, Patch, Delete }
		    public interface IEndpointAdapter
		    {
		        HttpVerb Verb { get; }
		        string Pattern { get; }
		        string? AuthorizationPolicy => null;
		    }
		}
		""";

	private static readonly MetadataReference[] BasicReferences =
		((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
		.Split(Path.PathSeparator)
		.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
		.ToArray();

	internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
	{
		var syntaxTrees = new[]
		{
			CSharpSyntaxTree.ParseText(StubTypes, path: "Stubs.cs"),
			CSharpSyntaxTree.ParseText(source, path: "Test.cs"),
		};

		var compilation = CSharpCompilation.Create(
			"TestAssembly",
			syntaxTrees,
			BasicReferences,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var compilationWithAnalyzers = compilation.WithAnalyzers(
			ImmutableArray.Create<DiagnosticAnalyzer>(new EndpointRouteAnalyzer()));

		return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
	}
}
