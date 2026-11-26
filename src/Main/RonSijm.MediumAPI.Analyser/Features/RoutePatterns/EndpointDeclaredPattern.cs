using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RonSijm.MediumAPI.Analyser;

internal sealed record DeclaredEndpointPattern(bool HasAttribute, string? Pattern = null, string? Problem = null, LocationInfo? Location = null);

/// <summary>Reads and validates one declarative endpoint pattern for generation and source-located route analysis.</summary>
internal static class EndpointDeclaredPattern
{
    internal static DeclaredEndpointPattern Read(Compilation compilation, INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        var attributeType = compilation.GetTypeByMetadataName("RonSijm.MediumAPI.EndpointPatternAttribute");
        if (attributeType is null)
        {
            return new(false);
        }

        var attributes = type.GetAttributes().Where(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType)).ToArray();
        if (attributes.Length == 0)
        {
            return new(false);
        }

        var syntax = attributes[0].ApplicationSyntaxReference?.GetSyntax(cancellationToken) as AttributeSyntax;
        var location = syntax is null ? null : LocationInfo.From(syntax.ArgumentList?.Arguments.FirstOrDefault()?.Expression ?? (SyntaxNode)syntax);
        string? pattern = null;
        string? problem = null;
        if (attributes.Length != 1)
        {
            problem = "declare EndpointPattern only once";
        }
        else if (attributes[0].ConstructorArguments.Length != 1 || attributes[0].ConstructorArguments[0].Kind == TypedConstantKind.Error || attributes[0].ConstructorArguments[0].Value is not string value)
        {
            problem = "EndpointPattern requires a non-null compile-time string pattern; an empty string is allowed";
        }
        else
        {
            pattern = value;
        }

        if (problem is null && (type.TypeKind != TypeKind.Class || type.IsRecord || type.IsAbstract || type.IsStatic))
        {
            problem = "EndpointPattern requires an ordinary nonabstract, nonstatic partial class";
        }

        if (problem is null && type.Name == "Pattern")
        {
            problem = "an EndpointPattern class cannot be named Pattern because that is the generated property name";
        }

        if (problem is null)
        {
            problem = EndpointRegistrationGenerator.GetDeclarationProblem(type);
        }

        if (problem is null && type.ContainingType is not null)
        {
            problem = "EndpointPattern currently requires a top-level class; nested endpoints can keep an explicit Pattern property";
        }

        var endpointInterface = compilation.GetTypeByMetadataName("RonSijm.MediumAPI.IEndpoint");
        if (problem is null && (endpointInterface is null || !type.AllInterfaces.Any(iface => SymbolEqualityComparer.Default.Equals(iface, endpointInterface)) || EndpointRegistrationGenerator.InferVerbFromInterfaces(compilation, type) is null))
        {
            problem = "EndpointPattern requires IEndpoint and one unambiguous supported verb endpoint interface";
        }

        if (problem is null && (type.DeclaringSyntaxReferences.Length == 0 || type.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax(cancellationToken) is not ClassDeclarationSyntax declaration || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword))))
        {
            problem = "EndpointPattern requires every declaration of its class to be partial";
        }

        if (problem is null)
        {
            for (var current = type; current is not null && problem is null; current = current.BaseType)
            {
                if (!SymbolEqualityComparer.Default.Equals(current, type) && current.DeclaringSyntaxReferences.Any(reference => compilation.ContainsSyntaxTree(reference.SyntaxTree)))
                {
                    var inherited = Read(compilation, current, cancellationToken);
                    if (inherited.HasAttribute && inherited.Problem is null)
                    {
                        problem = "EndpointPattern cannot be combined with an inherited generated Pattern property; keep the inherited pattern or an explicit implementation";
                        break;
                    }
                }

                foreach (var member in current.GetMembers().Where(IsPatternMember))
                {
                    if (SymbolEqualityComparer.Default.Equals(current, type) && member is IPropertySymbol property && IsGeneratedPattern(property, type, pattern!, cancellationToken))
                    {
                        continue;
                    }

                    problem = "EndpointPattern cannot be combined with a declared or inherited Pattern member; choose the attribute or an explicit Pattern implementation";
                    break;
                }
            }
        }

        return new(true, pattern, problem, location);
    }

    internal static DeclaredEndpointPattern? ReadInherited(Compilation compilation, INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var declared = Read(compilation, current, cancellationToken);
            if (current.GetMembers().Where(IsPatternMember).Any(member => !declared.HasAttribute || declared.Problem is not null || member is not IPropertySymbol property || !IsGeneratedPattern(property, current, declared.Pattern!, cancellationToken)))
            {
                return null;
            }

            if (!SymbolEqualityComparer.Default.Equals(current, type) && declared.HasAttribute)
            {
                return declared.Problem is null && current.DeclaringSyntaxReferences.Any(reference => compilation.ContainsSyntaxTree(reference.SyntaxTree)) ? declared : null;
            }
        }

        return null;
    }

    private static bool IsPatternMember(ISymbol member)
    {
        return member.Name == "Pattern"
               || member is IMethodSymbol method && method.ExplicitInterfaceImplementations.Any(implementation => implementation.Name == "Pattern")
               || member is IPropertySymbol property && property.ExplicitInterfaceImplementations.Any(implementation => implementation.Name == "Pattern")
               || member is IEventSymbol eventSymbol && eventSymbol.ExplicitInterfaceImplementations.Any(implementation => implementation.Name == "Pattern");
    }

    internal static string GetHintName(INamedTypeSymbol type)
    {
        using var hash = SHA256.Create();
        var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        return "EndpointPattern." + string.Concat(bytes.Select(value => value.ToString("x2", CultureInfo.InvariantCulture))) + ".g.cs";
    }

    internal static string GeneratePatternSource(INamedTypeSymbol type, string pattern)
    {
        var source = new StringBuilder();
        source.AppendLine("// <auto-generated/>");
        source.AppendLine("#nullable enable");
        var namespaces = new Stack<string>();
        for (var current = type.ContainingNamespace; !current.IsGlobalNamespace; current = current.ContainingNamespace)
        {
            namespaces.Push("@" + current.Name);
        }

        if (namespaces.Count > 0)
        {
            source.AppendLine("namespace " + string.Join(".", namespaces));
            source.AppendLine("{");
        }

        var visibility = type.DeclaredAccessibility == Accessibility.Public ? "public" : "internal";
        source.AppendLine(visibility + " partial class @" + type.Name);
        source.AppendLine("{");
        source.AppendLine("    [global::System.Diagnostics.CodeAnalysis.StringSyntax(\"Route\")]");
        source.AppendLine("    public string Pattern => " + Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(pattern, true) + ";");
        source.AppendLine("}");
        if (namespaces.Count > 0)
        {
            source.AppendLine("}");
        }

        return source.ToString();
    }

    private static bool IsGeneratedPattern(IPropertySymbol property, INamedTypeSymbol type, string pattern, CancellationToken cancellationToken)
    {
        if (property.DeclaredAccessibility != Accessibility.Public || property.IsStatic || property.IsIndexer || property.Type.SpecialType != SpecialType.System_String || property.SetMethod is not null || property.DeclaringSyntaxReferences.Length != 1)
        {
            return false;
        }

        var reference = property.DeclaringSyntaxReferences[0];
        return reference.SyntaxTree.FilePath.EndsWith(GetHintName(type), StringComparison.Ordinal)
               && reference.SyntaxTree.GetText(cancellationToken).ToString() == GeneratePatternSource(type, pattern);
    }
}