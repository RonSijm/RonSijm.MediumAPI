using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

public sealed class EndpointRouteAnalyzerTests
{
	// ──────────────────────────────────────────────────────────────
	// ENDPOINT001 — route parameter missing from HandleAsync
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task ENDPOINT001_RouteHasParameter_HandlerHasNo_Reports()
	{
		const string source = """
			using System.Diagnostics.CodeAnalysis;
			using System.Threading;
			using System.Threading.Tasks;
			using RonSijm.MediumAPI;

			internal sealed class MissingParamEndpoint : IEndpoint
			{
			    public HttpVerb Verb => HttpVerb.Get;
			    [StringSyntax("Route")]
			    public string Pattern => "items/{id:int}";
			    public Task HandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().Contain(d => d.Id == EndpointDiagnosticIds.RouteParameterMissingFromHandler);
	}

	[Fact]
	public async Task ENDPOINT001_RouteHasParameter_HandlerHasIt_NoReport()
	{
		const string source = """
			using System.Diagnostics.CodeAnalysis;
			using System.Threading;
			using System.Threading.Tasks;
			using RonSijm.MediumAPI;

			internal sealed class ValidEndpoint : IEndpoint
			{
			    public HttpVerb Verb => HttpVerb.Get;
			    [StringSyntax("Route")]
			    public string Pattern => "items/{id:int}";
			    public Task HandleAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().NotContain(d => d.Id == EndpointDiagnosticIds.RouteParameterMissingFromHandler);
	}

	// ──────────────────────────────────────────────────────────────
	// ENDPOINT002 — handler parameter not present in route
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task ENDPOINT002_HandlerHasParameter_RouteHasNone_Reports()
	{
		const string source = """
			using System.Diagnostics.CodeAnalysis;
			using System.Threading;
			using System.Threading.Tasks;
			using RonSijm.MediumAPI;

			internal sealed class ExtraParamEndpoint : IEndpoint
			{
			    public HttpVerb Verb => HttpVerb.Get;
			    [StringSyntax("Route")]
			    public string Pattern => "items";
			    public Task HandleAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().Contain(d => d.Id == EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
	}

	[Fact]
	public async Task ENDPOINT002_HandlerHasParameter_RouteHasIt_NoReport()
	{
		const string source = """
			using System.Diagnostics.CodeAnalysis;
			using System.Threading;
			using System.Threading.Tasks;
			using RonSijm.MediumAPI;

			internal sealed class ValidEndpoint : IEndpoint
			{
			    public HttpVerb Verb => HttpVerb.Get;
			    [StringSyntax("Route")]
			    public string Pattern => "items/{id:int}";
			    public Task HandleAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().NotContain(d => d.Id == EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
	}

	[Fact]
	public async Task ENDPOINT002_HandlerHasComplexBodyParameter_NoReport()
	{
		const string source = """
			using System.Diagnostics.CodeAnalysis;
			using System.Threading;
			using System.Threading.Tasks;
			using RonSijm.MediumAPI;

			internal sealed record CreateItemRequest(string Name);

			internal sealed class CreateItemEndpoint : IEndpoint
			{
			    public HttpVerb Verb => HttpVerb.Post;
			    [StringSyntax("Route")]
			    public string Pattern => "items";
			    public Task HandleAsync(CreateItemRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().NotContain(d => d.Id == EndpointDiagnosticIds.HandlerParameterMissingFromRoute);
	}

	// ──────────────────────────────────────────────────────────────
	// ENDPOINT003 — route constraint type mismatch
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task ENDPOINT003_RouteConstraintInt_HandlerParameterString_Reports()
	{
		const string source = """
			using System.Diagnostics.CodeAnalysis;
			using System.Threading;
			using System.Threading.Tasks;
			using RonSijm.MediumAPI;

			internal sealed class TypeMismatchEndpoint : IEndpoint
			{
			    public HttpVerb Verb => HttpVerb.Get;
			    [StringSyntax("Route")]
			    public string Pattern => "items/{id:int}";
			    public Task HandleAsync(string id, CancellationToken cancellationToken) => Task.CompletedTask;
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().Contain(d => d.Id == EndpointDiagnosticIds.RouteConstraintTypeMismatch);
	}

	[Fact]
	public async Task ENDPOINT003_RouteConstraintInt_HandlerParameterInt_NoReport()
	{
		const string source = """
			using System.Diagnostics.CodeAnalysis;
			using System.Threading;
			using System.Threading.Tasks;
			using RonSijm.MediumAPI;

			internal sealed class ValidEndpoint : IEndpoint
			{
			    public HttpVerb Verb => HttpVerb.Get;
			    [StringSyntax("Route")]
			    public string Pattern => "items/{id:int}";
			    public Task HandleAsync(int id, CancellationToken cancellationToken) => Task.CompletedTask;
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().NotContain(d => d.Id == EndpointDiagnosticIds.RouteConstraintTypeMismatch);
	}

	// ──────────────────────────────────────────────────────────────
	// ENDPOINT005 — Pattern property missing [StringSyntax("Route")]
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task ENDPOINT005_PatternMissingStringSyntaxAttribute_Reports()
	{
		const string source = """
			using System.Threading;
			using System.Threading.Tasks;
			using RonSijm.MediumAPI;

			internal sealed class NoStringSyntaxEndpoint : IEndpoint
			{
			    public HttpVerb Verb => HttpVerb.Get;
			    public string Pattern => "items";
			    public Task HandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().Contain(d => d.Id == EndpointDiagnosticIds.PatternPropertyMissingStringSyntaxAttribute);
	}

	[Fact]
	public async Task ENDPOINT005_PatternHasStringSyntaxAttribute_NoReport()
	{
		const string source = """
			using System.Diagnostics.CodeAnalysis;
			using System.Threading;
			using System.Threading.Tasks;
			using RonSijm.MediumAPI;

			internal sealed class ValidEndpoint : IEndpoint
			{
			    public HttpVerb Verb => HttpVerb.Get;
			    [StringSyntax("Route")]
			    public string Pattern => "items";
			    public Task HandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().NotContain(d => d.Id == EndpointDiagnosticIds.PatternPropertyMissingStringSyntaxAttribute);
	}

	// ──────────────────────────────────────────────────────────────
	// ENDPOINT007 — HandleAsync method missing
	// ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task ENDPOINT007_HandleAsyncMissing_Reports()
	{
		const string source = """
			using System.Diagnostics.CodeAnalysis;
			using RonSijm.MediumAPI;

			internal sealed class NoHandleAsyncEndpoint : IEndpoint
			{
			    public HttpVerb Verb => HttpVerb.Get;
			    [StringSyntax("Route")]
			    public string Pattern => "items";
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().Contain(d => d.Id == EndpointDiagnosticIds.HandleAsyncMethodMissing);
	}

	[Fact]
	public async Task ENDPOINT007_HandleAsyncPresent_NoReport()
	{
		const string source = """
			using System.Diagnostics.CodeAnalysis;
			using System.Threading;
			using System.Threading.Tasks;
			using RonSijm.MediumAPI;

			internal sealed class ValidEndpoint : IEndpoint
			{
			    public HttpVerb Verb => HttpVerb.Get;
			    [StringSyntax("Route")]
			    public string Pattern => "items";
			    public Task HandleAsync(CancellationToken cancellationToken) => Task.CompletedTask;
			}
			""";

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

		diagnostics.Should().NotContain(d => d.Id == EndpointDiagnosticIds.HandleAsyncMethodMissing);
	}
}
