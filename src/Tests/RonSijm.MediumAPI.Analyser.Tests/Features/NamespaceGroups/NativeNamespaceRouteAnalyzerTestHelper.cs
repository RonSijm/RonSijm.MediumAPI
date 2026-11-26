using System.Collections.Immutable;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace RonSijm.MediumAPI.Analyser.Tests;

/// <summary>Uses real ASP.NET Core symbols; only the library declaration contracts are stubbed.</summary>
internal static class NativeNamespaceRouteAnalyzerTestHelper
{
    private const string LibraryContracts = """
                                            using Microsoft.AspNetCore.Routing;
                                            namespace RonSijm.MediumAPI
                                            {
                                                public enum HttpVerb { Get }
                                                public interface IEndpoint { HttpVerb Verb { get; } string Pattern { get; } }
                                                public interface IManuallyMappedEndpoint : IEndpoint { }
                                                public sealed class EndpointMappingContext { }
                                                public interface INamespaceEndpointGroup
                                                {
                                                    static abstract RouteGroupBuilder Map(IEndpointRouteBuilder endpoints);
                                                    static virtual bool ShouldMap(EndpointMappingContext context) => true;
                                                }
                                            }
                                            """;

    internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
    {
        var referencePaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Concat(new[] { typeof(EndpointRouteBuilderExtensions).Assembly.Location, typeof(RouteGroupBuilder).Assembly.Location, typeof(FromRouteAttribute).Assembly.Location })
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var compilation = CSharpCompilation.Create("NativeNamespaceRouteFixture",
            new[] { CSharpSyntaxTree.ParseText(LibraryContracts, path: "LibraryContracts.cs"), CSharpSyntaxTree.ParseText(source, path: "Endpoint.cs") },
            referencePaths.Select(file => MetadataReference.CreateFromFile(file)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        // A broken fixture must not masquerade as an unknown layout and make a negative case pass.
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EndpointRouteAnalyzer())).GetAnalyzerDiagnosticsAsync();
    }
}