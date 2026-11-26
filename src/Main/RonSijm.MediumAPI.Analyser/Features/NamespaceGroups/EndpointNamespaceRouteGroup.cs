using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RonSijm.MediumAPI.Analyser;

internal sealed record DeclaredNamespaceRouteGroup(bool HasAttribute, string? Prefix = null, string? Problem = null, LocationInfo? Location = null);

/// <summary>Shares declaration validation between map generation and conservative route-layout analysis.</summary>
internal static class EndpointNamespaceRouteGroup
{
    internal static bool HasAttribute(Compilation compilation, INamedTypeSymbol type)
    {
        var attributeType = compilation.GetTypeByMetadataName("RonSijm.MediumAPI.NamespaceRouteGroupAttribute");
        return attributeType is not null && type.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType));
    }

    internal static DeclaredNamespaceRouteGroup Read(Compilation compilation, INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        var attributeType = compilation.GetTypeByMetadataName("RonSijm.MediumAPI.NamespaceRouteGroupAttribute");
        if (attributeType is null)
        {
            return new(false);
        }

        var attributes = type.GetAttributes().Where(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType)).ToArray();
        if (attributes.Length == 0)
        {
            return new(false);
        }

        var location = attributes[0].ApplicationSyntaxReference?.GetSyntax(cancellationToken) is { } syntax ? LocationInfo.From(syntax) : null;
        string? problem = null;
        string? prefix = null;
        if (attributes.Length != 1)
        {
            problem = "declare NamespaceRouteGroup only once";
        }
        else if (attributes[0].ConstructorArguments.Length != 1 || attributes[0].ConstructorArguments[0].Kind == TypedConstantKind.Error || attributes[0].ConstructorArguments[0].Value is not string value)
        {
            problem = "NamespaceRouteGroup requires a non-null compile-time string prefix; an empty string is allowed";
        }
        else
        {
            prefix = value;
        }

        if (problem is null && (type.TypeKind != TypeKind.Class || type.IsRecord || type.IsAbstract || type.IsStatic))
        {
            problem = "NamespaceRouteGroup requires an ordinary nonabstract, nonstatic partial class";
        }

        if (problem is null && type.Name == "Map")
        {
            problem = "a NamespaceRouteGroup class cannot be named Map because that is the generated method name";
        }

        if (problem is null)
        {
            problem = EndpointRegistrationGenerator.GetDeclarationProblem(type);
        }

        if (problem is null && type.ContainingType is not null)
        {
            problem = "NamespaceRouteGroup currently requires a top-level class; nested groups can keep an explicit Map method";
        }

        var groupInterface = compilation.GetTypeByMetadataName("RonSijm.MediumAPI.INamespaceEndpointGroup");
        if (problem is null && (groupInterface is null || !type.AllInterfaces.Any(iface => SymbolEqualityComparer.Default.Equals(iface, groupInterface))))
        {
            problem = "NamespaceRouteGroup requires INamespaceEndpointGroup";
        }

        if (problem is null && (type.DeclaringSyntaxReferences.Length == 0 || type.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax(cancellationToken) is not ClassDeclarationSyntax declaration || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword))))
        {
            problem = "NamespaceRouteGroup requires every declaration of its class to be partial";
        }

        if (problem is null)
        {
            for (var current = type; current is not null && problem is null; current = current.BaseType)
            {
                foreach (var member in current.GetMembers().Where(IsMapMember))
                {
                    if (SymbolEqualityComparer.Default.Equals(current, type) && member is IMethodSymbol method && IsGeneratedMap(method, type, prefix!, cancellationToken))
                    {
                        continue;
                    }

                    problem = "NamespaceRouteGroup cannot be combined with a declared or inherited Map member; choose the attribute or an explicit Map implementation";
                    break;
                }
            }
        }

        if (problem is null)
        {
            var conditions = EndpointRegistrationGenerator.GetMethodsInHierarchy(type, "ShouldMap").ToArray();
            if (conditions.Length > 1 || conditions.Any(method => !EndpointRegistrationGenerator.IsGroupMethod(method, "bool", "RonSijm.MediumAPI.EndpointMappingContext")))
            {
                problem = "ShouldMap must be public static bool ShouldMap(EndpointMappingContext context) when declared";
            }
        }

        return new(true, prefix, problem, location);
    }

    private static bool IsMapMember(ISymbol member)
    {
        return member.Name == "Map"
               || member is IMethodSymbol method && method.ExplicitInterfaceImplementations.Any(implementation => implementation.Name == "Map")
               || member is IPropertySymbol property && property.ExplicitInterfaceImplementations.Any(implementation => implementation.Name == "Map")
               || member is IEventSymbol eventSymbol && eventSymbol.ExplicitInterfaceImplementations.Any(implementation => implementation.Name == "Map");
    }

    internal static string GetHintName(INamedTypeSymbol type)
    {
        using var hash = SHA256.Create();
        var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        return "NamespaceRouteGroup." + string.Concat(bytes.Select(value => value.ToString("x2", CultureInfo.InvariantCulture))) + ".g.cs";
    }

    internal static string GenerateMapSource(INamedTypeSymbol type, string prefix)
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
        source.AppendLine("    public static global::Microsoft.AspNetCore.Routing.RouteGroupBuilder Map(global::Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)");
        source.AppendLine("    {");
        source.AppendLine("        return global::Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapGroup(endpoints, " + Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(prefix, true) + ");");
        source.AppendLine("    }");
        source.AppendLine("}");
        if (namespaces.Count > 0)
        {
            source.AppendLine("}");
        }

        return source.ToString();
    }

    private static bool IsGeneratedMap(IMethodSymbol method, INamedTypeSymbol type, string prefix, CancellationToken cancellationToken)
    {
        if (!EndpointRegistrationGenerator.IsGroupMethod(method, "Microsoft.AspNetCore.Routing.RouteGroupBuilder", "Microsoft.AspNetCore.Routing.IEndpointRouteBuilder") || method.DeclaringSyntaxReferences.Length != 1)
        {
            return false;
        }

        var reference = method.DeclaringSyntaxReferences[0];
        if (!reference.SyntaxTree.FilePath.EndsWith(GetHintName(type), StringComparison.Ordinal) || reference.GetSyntax(cancellationToken) is not MethodDeclarationSyntax declaration || declaration.Body is not { Statements.Count: 1 } body || body.Statements[0] is not ReturnStatementSyntax { Expression: InvocationExpressionSyntax call })
        {
            return false;
        }

        return call.Expression.ToString() == "global::Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions.MapGroup" && call.ArgumentList.Arguments.Count == 2
                                                                                                                            && call.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == method.Parameters[0].Name
                                                                                                                            && call.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax literal && literal.Token.ValueText == prefix;
    }
}