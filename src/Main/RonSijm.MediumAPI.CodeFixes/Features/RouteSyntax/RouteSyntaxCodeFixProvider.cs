using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RonSijm.MediumAPI.CodeFixes;

/// <summary>Adds the route-tooling annotation without changing the endpoint contract.</summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RouteSyntaxCodeFixProvider))]
[Shared]
public sealed class RouteSyntaxCodeFixProvider : CodeFixProvider
{
    private const string DiagnosticId = "ENDPOINT005";
    private const string EquivalenceKey = "MediumAPI.AddRouteSyntaxAnnotation";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null || semanticModel is null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id != DiagnosticId || !diagnostic.Location.IsInSource || diagnostic.Location.SourceTree != root.SyntaxTree)
            {
                continue;
            }

            var span = diagnostic.Location.SourceSpan;
            if (GetFixableProperty(root, semanticModel, span, context.CancellationToken) is null)
            {
                continue;
            }

            context.RegisterCodeFix(CodeAction.Create("Add route syntax annotation", cancellationToken => AddRouteSyntaxAttributeAsync(context.Document, span, cancellationToken), EquivalenceKey), diagnostic);
        }
    }

    private static PropertyDeclarationSyntax? GetFixableProperty(SyntaxNode root, SemanticModel semanticModel, TextSpan diagnosticSpan, CancellationToken cancellationToken)
    {
        if (diagnosticSpan.IsEmpty || !root.FullSpan.Contains(diagnosticSpan))
        {
            return null;
        }

        var property = root.FindToken(diagnosticSpan.Start).Parent?.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
        if (property is null || property.ContainsDiagnostics || property.Identifier.ValueText != "Pattern" || property.Identifier.Span != diagnosticSpan)
        {
            return null;
        }

        if (semanticModel.GetDeclaredSymbol(property, cancellationToken) is not IPropertySymbol symbol || symbol.Type.SpecialType != SpecialType.System_String)
        {
            return null;
        }

        var endpointType = semanticModel.Compilation.GetTypeByMetadataName("RonSijm.MediumAPI.IEndpoint");
        var annotationType = semanticModel.Compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.StringSyntaxAttribute");
        if (endpointType is null || annotationType is null || !symbol.ContainingType.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, endpointType)))
        {
            return null;
        }

        // StringSyntaxAttribute is not repeatable. Preserve both an equivalent Route
        // annotation and any other existing syntax annotation for manual review.
        if (symbol.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, annotationType)))
        {
            return null;
        }

        return property;
    }

    private static async Task<Document> AddRouteSyntaxAttributeAsync(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || semanticModel is null)
        {
            return document;
        }

        var property = GetFixableProperty(root, semanticModel, diagnosticSpan, cancellationToken);
        if (property is null)
        {
            return document;
        }

        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var line = sourceText.Lines.GetLineFromPosition(property.SpanStart);
        var indentation = new string(sourceText.ToString(TextSpan.FromBounds(line.Start, property.SpanStart)).TakeWhile(character => character is ' ' or '\t').ToArray());

        var annotation = SyntaxFactory.Attribute(SyntaxFactory.ParseName("global::System.Diagnostics.CodeAnalysis.StringSyntax"))
            .WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("Route"))))));
        var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(annotation))
            .WithTrailingTrivia(SyntaxFactory.EndOfLine(GetNewLine(sourceText, line)), SyntaxFactory.Whitespace(indentation));

        // Move only the existing leading trivia to the new first attribute. This keeps
        // documentation/comments above the declaration and retains the original body,
        // existing attributes and their trivia without reformatting the property.
        var withoutLeadingTrivia = property.WithoutLeadingTrivia();
        var updated = withoutLeadingTrivia.WithAttributeLists(withoutLeadingTrivia.AttributeLists.Insert(0, attributeList)).WithLeadingTrivia(property.GetLeadingTrivia());
        return document.WithSyntaxRoot(root.ReplaceNode(property, updated));
    }

    private static string GetNewLine(SourceText sourceText, TextLine propertyLine)
    {
        if (propertyLine.EndIncludingLineBreak > propertyLine.End)
        {
            return sourceText.ToString(TextSpan.FromBounds(propertyLine.End, propertyLine.EndIncludingLineBreak));
        }

        foreach (var line in sourceText.Lines)
        {
            if (line.EndIncludingLineBreak > line.End)
            {
                return sourceText.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
            }
        }

        return "\n";
    }
}