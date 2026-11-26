using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using RonSijm.MediumAPI.Analyser;
using Xunit;

namespace RonSijm.MediumAPI.CodeFixes.Tests;

public sealed class RouteSyntaxCodeFixProviderTests
{
    private static readonly DiagnosticDescriptor MissingAnnotation = new("ENDPOINT005", "Missing route annotation", "Missing route annotation", "EndpointRouting", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    [Fact]
    public async Task MissingAnnotation_EditPreservesRouteMetadataCommentsAndCrLf_AndClearsAnalyzerWarning()
    {
        var source = """
                     using System;

                     public sealed class SampleEndpoint : RonSijm.MediumAPI.IEndpoint
                     {
                         /// <summary>Keep the public route documentation.</summary>
                         [Obsolete("Keep existing metadata")]
                         public string Pattern => "/items/{id:long}"; // Keep the route comment.

                         public string HandleAsync(long id) => id.ToString();
                     }
                     """.Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n";
        using var fixture = SourceDocument.Create(source);
        await AssertCompilesAsync(fixture.Document);
        var diagnostic = Assert.Single((await GetAnalyzerDiagnosticsAsync(fixture.Document)).Where(candidate => candidate.Id == "ENDPOINT005"));
        var action = Assert.Single(await GetActionsAsync(fixture.Document, diagnostic));

        var updated = await ApplyAsync(fixture.Document, action);

        var expected = source.Replace("    [Obsolete(\"Keep existing metadata\")]", "    [global::System.Diagnostics.CodeAnalysis.StringSyntax(\"Route\")]\r\n    [Obsolete(\"Keep existing metadata\")]");
        Assert.Equal(expected, (await updated.GetTextAsync()).ToString());
        await AssertCompilesAsync(updated);
        Assert.DoesNotContain(await GetAnalyzerDiagnosticsAsync(updated), candidate => candidate.Id == "ENDPOINT005");
        Assert.Empty(await GetActionsAsync(updated, await DiagnosticForPropertyAsync(updated, "Pattern")));
    }

    [Theory]
    [InlineData("using System.Diagnostics.CodeAnalysis;", "[StringSyntax(\"Route\")]")]
    [InlineData("", "[global::System.Diagnostics.CodeAnalysis.StringSyntax(\"Route\")]")]
    [InlineData("using RouteSyntax = System.Diagnostics.CodeAnalysis.StringSyntaxAttribute;", "[RouteSyntax(SyntaxName)]")]
    public async Task ExistingRouteAnnotation_IncludingAliasAndConstant_OffersNoDuplicate(string imports, string annotation)
    {
        var source = $$"""
                       {{imports}}
                       public sealed class SampleEndpoint : RonSijm.MediumAPI.IEndpoint
                       {
                           private const string SyntaxName = "Route";
                           {{annotation}}
                           public string Pattern => "/status";
                           public string HandleAsync() => "ready";
                       }
                       """;
        using var fixture = SourceDocument.Create(source);
        await AssertCompilesAsync(fixture.Document);

        Assert.Empty(await GetActionsAsync(fixture.Document, await DiagnosticForPropertyAsync(fixture.Document, "Pattern")));
        Assert.DoesNotContain(await GetAnalyzerDiagnosticsAsync(fixture.Document), candidate => candidate.Id == "ENDPOINT005");
    }

    [Fact]
    public async Task ExistingDifferentSyntaxAnnotation_IsLeftForManualReview()
    {
        const string source = """
                              public sealed class SampleEndpoint : RonSijm.MediumAPI.IEndpoint
                              {
                                  [System.Diagnostics.CodeAnalysis.StringSyntax("Regex")]
                                  public string Pattern => "/status";
                                  public string HandleAsync() => "ready";
                              }
                              """;
        using var fixture = SourceDocument.Create(source);
        await AssertCompilesAsync(fixture.Document);
        var diagnostic = Assert.Single((await GetAnalyzerDiagnosticsAsync(fixture.Document)).Where(candidate => candidate.Id == "ENDPOINT005"));

        Assert.Empty(await GetActionsAsync(fixture.Document, diagnostic));
    }

    [Fact]
    public async Task PatternOnUnrelatedType_OffersNoFix()
    {
        using var fixture = SourceDocument.Create("public sealed class Settings { public string Pattern => \"/status\"; }");
        await AssertCompilesAsync(fixture.Document);

        Assert.Empty(await GetActionsAsync(fixture.Document, await DiagnosticForPropertyAsync(fixture.Document, "Pattern")));
    }

    [Fact]
    public async Task DiagnosticOnAnotherProperty_OffersNoFix()
    {
        const string source = """
                              public sealed class SampleEndpoint : RonSijm.MediumAPI.IEndpoint
                              {
                                  public string Pattern => "/status";
                                  public string Description => "status endpoint";
                              }
                              """;
        using var fixture = SourceDocument.Create(source);
        await AssertCompilesAsync(fixture.Document);

        Assert.Empty(await GetActionsAsync(fixture.Document, await DiagnosticForPropertyAsync(fixture.Document, "Description")));
    }

    [Fact]
    public async Task DiagnosticFromDifferentSyntaxTree_OffersNoFix()
    {
        const string source = "public sealed class SampleEndpoint : RonSijm.MediumAPI.IEndpoint { public string Pattern => \"/status\"; }";
        using var fixture = SourceDocument.Create(source);
        await AssertCompilesAsync(fixture.Document);
        var otherTree = CSharpSyntaxTree.ParseText(source);
        var otherProperty = Assert.Single((await otherTree.GetRootAsync()).DescendantNodes().OfType<PropertyDeclarationSyntax>());
        var diagnostic = Diagnostic.Create(MissingAnnotation, otherProperty.Identifier.GetLocation());

        Assert.Empty(await GetActionsAsync(fixture.Document, diagnostic));
    }

    [Fact]
    public async Task DiagnosticOnPropertyType_OffersNoFix()
    {
        using var fixture = SourceDocument.Create("public sealed class SampleEndpoint : RonSijm.MediumAPI.IEndpoint { public string Pattern => \"/status\"; }");
        await AssertCompilesAsync(fixture.Document);
        var root = await fixture.Document.GetSyntaxRootAsync();
        var property = Assert.Single(root!.DescendantNodes().OfType<PropertyDeclarationSyntax>());
        var diagnostic = Diagnostic.Create(MissingAnnotation, property.Type.GetLocation());

        Assert.Empty(await GetActionsAsync(fixture.Document, diagnostic));
    }

    [Fact]
    public async Task OtherDiagnosticId_OffersNoContractChangingFix()
    {
        using var fixture = SourceDocument.Create("public sealed class SampleEndpoint : RonSijm.MediumAPI.IEndpoint { public string Pattern => \"/status\"; }");
        await AssertCompilesAsync(fixture.Document);
        var missing = await DiagnosticForPropertyAsync(fixture.Document, "Pattern");
        var descriptor = new DiagnosticDescriptor("ENDPOINT001", "Route binding", "Route binding", "EndpointRouting", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        Assert.Empty(await GetActionsAsync(fixture.Document, Diagnostic.Create(descriptor, missing.Location)));
    }

    private static async Task<Diagnostic> DiagnosticForPropertyAsync(Document document, string propertyName)
    {
        var root = await document.GetSyntaxRootAsync();
        var property = Assert.Single(root!.DescendantNodes().OfType<PropertyDeclarationSyntax>().Where(candidate => candidate.Identifier.ValueText == propertyName));
        return Diagnostic.Create(MissingAnnotation, property.Identifier.GetLocation());
    }

    private static async Task<IReadOnlyList<CodeAction>> GetActionsAsync(Document document, Diagnostic diagnostic)
    {
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await new RouteSyntaxCodeFixProvider().RegisterCodeFixesAsync(context);
        return actions;
    }

    private static async Task<Document> ApplyAsync(Document original, CodeAction action)
    {
        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var operation = Assert.Single(operations.OfType<ApplyChangesOperation>());
        return operation.ChangedSolution.GetDocument(original.Id)!;
    }

    private static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(Document document)
    {
        var compilation = await document.Project.GetCompilationAsync();
        return await compilation!.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new EndpointRouteAnalyzer())).GetAnalyzerDiagnosticsAsync();
    }

    private static async Task AssertCompilesAsync(Document document)
    {
        var compilation = await document.Project.GetCompilationAsync();
        Assert.NotNull(compilation);
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors.Select(diagnostic => diagnostic.ToString())));
    }

    private sealed class SourceDocument : IDisposable
    {
        private readonly AdhocWorkspace _workspace;

        private SourceDocument(AdhocWorkspace workspace, Document document)
        {
            _workspace = workspace;
            Document = document;
        }

        public Document Document { get; }

        public static SourceDocument Create(string source)
        {
            const string endpointContract = "namespace RonSijm.MediumAPI { public interface IEndpoint { string Pattern { get; } } }";
            var trustedPaths = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? throw new InvalidOperationException("Runtime reference paths are unavailable.")).Split(Path.PathSeparator);
            var runtimeFacade = trustedPaths.Single(path => Path.GetFileName(path).Equals("System.Runtime.dll", StringComparison.OrdinalIgnoreCase));
            var references = new[] { typeof(object).Assembly.Location, typeof(StringSyntaxAttribute).Assembly.Location, runtimeFacade }
                .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path));
            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject("CodeFixSource", LanguageNames.CSharp)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithParseOptions(new CSharpParseOptions(LanguageVersion.Latest))
                .WithMetadataReferences(references);
            var contract = project.AddDocument("IEndpoint.cs", SourceText.From(endpointContract));
            var document = contract.Project.AddDocument("Endpoint.cs", SourceText.From(source));
            Assert.True(workspace.TryApplyChanges(document.Project.Solution));
            return new SourceDocument(workspace, workspace.CurrentSolution.GetDocument(document.Id)!);
        }

        public void Dispose() => _workspace.Dispose();
    }
}