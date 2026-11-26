using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace RonSijm.MediumAPI.Analyser;

/// <summary>Reads a deliberately small, compiler-proven namespace layout without executing host or group code.</summary>
internal static class EndpointNamespaceRouteLayout
{
    private static readonly Regex SimpleToken = new(@"\{[a-zA-Z_][a-zA-Z0-9_]*(?::[a-zA-Z_][a-zA-Z0-9_]*(?:\([^{}]*\))?)*\??\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static bool TryGetPrefixes(Compilation compilation, INamedTypeSymbol endpointType, CancellationToken cancellationToken, out ImmutableArray<string> prefixes)
    {
        prefixes = default;
        if (endpointType.AllInterfaces.Any(iface => iface.ToDisplayString() == "RonSijm.MediumAPI.IManuallyMappedEndpoint"))
        {
            return false;
        }

        var groupInterface = compilation.GetTypeByMetadataName("RonSijm.MediumAPI.INamespaceEndpointGroup");
        if (groupInterface is null)
        {
            return false;
        }

        var groups = new Stack<ImmutableArray<string>>();
        for (var current = endpointType.ContainingNamespace; ; current = current.ContainingNamespace)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var definitions = GetTypes(current).Where(type => SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, endpointType.ContainingAssembly)
                                                              && (EndpointNamespaceRouteGroup.HasAttribute(compilation, type) || type.TypeKind == TypeKind.Class && !type.IsRecord && !type.IsAbstract
                                                                  && type.AllInterfaces.Any(iface => SymbolEqualityComparer.Default.Equals(iface, groupInterface)))).ToArray();
            if (definitions.Length > 0)
            {
                if (definitions.Length != 1 || !TryReadMap(compilation, definitions[0], cancellationToken, out var parts))
                {
                    return false;
                }

                groups.Push(parts);
            }

            if (current.IsGlobalNamespace)
            {
                break;
            }
        }

        if (groups.Count == 0)
        {
            return false;
        }

        prefixes = groups.SelectMany(group => group).ToImmutableArray();
        return true;
    }

    internal static bool IsReadablePattern(string pattern)
    {
        for (var index = 0; index < pattern.Length; index++)
        {
            if (pattern[index] == '}')
            {
                return false;
            }

            if (pattern[index] != '{')
            {
                continue;
            }

            var token = SimpleToken.Match(pattern, index);
            if (!token.Success || token.Index != index)
            {
                return false;
            }

            index += token.Length - 1;
        }

        return true;
    }

    private static IEnumerable<INamedTypeSymbol> GetTypes(INamespaceSymbol scope)
    {
        foreach (var type in scope.GetTypeMembers())
        {
            yield return type;
            foreach (var nested in GetNestedTypes(type))
            {
                yield return nested;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetNestedTypes(INamedTypeSymbol parent)
    {
        foreach (var type in parent.GetTypeMembers())
        {
            yield return type;
            foreach (var nested in GetNestedTypes(type))
            {
                yield return nested;
            }
        }
    }

    private static bool TryReadMap(Compilation compilation, INamedTypeSymbol definition, CancellationToken cancellationToken, out ImmutableArray<string> prefixes)
    {
        prefixes = default;
        var routeGroup = EndpointNamespaceRouteGroup.Read(compilation, definition, cancellationToken);
        if (routeGroup.HasAttribute)
        {
            if (routeGroup.Problem is not null || !IsReadablePattern(routeGroup.Prefix!))
            {
                return false;
            }

            prefixes = ImmutableArray.Create(routeGroup.Prefix!);
            return true;
        }

        if (definition.Arity != 0)
        {
            return false;
        }

        var methods = definition.GetMembers("Map").OfType<IMethodSymbol>().Where(method => method.IsStatic
                                                                                           && method.DeclaredAccessibility == Accessibility.Public && !method.IsGenericMethod && method.Parameters.Length == 1
                                                                                           && method.Parameters[0].Type.ToDisplayString() == "Microsoft.AspNetCore.Routing.IEndpointRouteBuilder"
                                                                                           && method.ReturnType.ToDisplayString() == "Microsoft.AspNetCore.Routing.RouteGroupBuilder").ToArray();
        if (methods.Length != 1 || methods[0].DeclaringSyntaxReferences.Length != 1)
        {
            return false;
        }

        var method = methods[0];
        if (method.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is not MethodDeclarationSyntax declaration)
        {
            return false;
        }

        var model = compilation.GetSemanticModel(declaration.SyntaxTree);
        ExpressionSyntax? expression = declaration.ExpressionBody?.Expression;
        if (expression is null && declaration.Body is { Statements.Count: 1 } body && body.Statements[0] is ReturnStatementSyntax returned)
        {
            expression = returned.Expression;
        }

        if (expression is null && declaration.Body is { Statements.Count: 2 } localBody
                               && localBody.Statements[0] is LocalDeclarationStatementSyntax local && local.Declaration.Variables.Count == 1
                               && localBody.Statements[1] is ReturnStatementSyntax { Expression: IdentifierNameSyntax identifier }
                               && SymbolEqualityComparer.Default.Equals(model.GetDeclaredSymbol(local.Declaration.Variables[0], cancellationToken), model.GetSymbolInfo(identifier, cancellationToken).Symbol))
        {
            expression = local.Declaration.Variables[0].Initializer?.Value;
        }

        if (expression is null)
        {
            return false;
        }

        var parts = ImmutableArray.CreateBuilder<string>();
        if (!TryReadBuilder(compilation, model, method.Parameters[0], expression, cancellationToken, parts, 0) || parts.Count == 0)
        {
            return false;
        }

        prefixes = parts.ToImmutable();
        return true;
    }

    private static bool TryReadBuilder(Compilation compilation, SemanticModel model, IParameterSymbol root, ExpressionSyntax expression, CancellationToken cancellationToken, ImmutableArray<string>.Builder prefixes, int depth)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (depth > 32)
        {
            return false;
        }

        if (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            return TryReadBuilder(compilation, model, root, parenthesized.Expression, cancellationToken, prefixes, depth + 1);
        }

        if (SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(expression, cancellationToken).Symbol, root))
        {
            return true;
        }

        if (expression is not InvocationExpressionSyntax invocation
            || model.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol called)
        {
            return false;
        }

        if (TryReadNativeConventionReceiver(compilation, model, invocation, called, cancellationToken, out var conventionReceiver))
        {
            return TryReadBuilder(compilation, model, root, conventionReceiver!, cancellationToken, prefixes, depth + 1);
        }

        var method = called.ReducedFrom ?? called;
        if (method.Name != "MapGroup" || method.ContainingType.ToDisplayString() != "Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions"
                                      || method.ContainingAssembly.Name != "Microsoft.AspNetCore.Routing" || method.Parameters.Length != 2
                                      || method.Parameters[0].Type.ToDisplayString() != "Microsoft.AspNetCore.Routing.IEndpointRouteBuilder"
                                      || method.Parameters[1].Type.SpecialType != SpecialType.System_String
                                      || method.ReturnType.ToDisplayString() != "Microsoft.AspNetCore.Routing.RouteGroupBuilder")
        {
            return false;
        }

        ExpressionSyntax? receiver;
        ExpressionSyntax? pattern;
        if (called.ReducedFrom is not null && invocation.Expression is MemberAccessExpressionSyntax member && invocation.ArgumentList.Arguments.Count == 1)
        {
            receiver = member.Expression;
            pattern = invocation.ArgumentList.Arguments[0].Expression;
        }
        else if (model.GetOperation(invocation, cancellationToken) is IInvocationOperation operation)
        {
            receiver = operation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 0)?.Value.Syntax as ExpressionSyntax;
            pattern = operation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 1)?.Value.Syntax as ExpressionSyntax;
        }
        else
        {
            return false;
        }

        if (receiver is null || pattern is null || !EndpointConstantValues.TryGetString(compilation, pattern, allowNull: false, out var prefix)
            || prefix is null || !IsReadablePattern(prefix)
            || !TryReadBuilder(compilation, model, root, receiver, cancellationToken, prefixes, depth + 1))
        {
            return false;
        }

        prefixes.Add(prefix);
        return true;
    }

    private static bool TryReadNativeConventionReceiver(Compilation compilation, SemanticModel model, InvocationExpressionSyntax invocation, IMethodSymbol called, CancellationToken cancellationToken, out ExpressionSyntax? receiver)
    {
        receiver = null;
        var method = (called.ReducedFrom ?? called).OriginalDefinition;
        if (!method.IsStatic || !method.IsExtensionMethod || method.Arity is not (1 or 2) || method.Parameters.Length == 0
            || method.DeclaringSyntaxReferences.Length != 0 || method.Parameters.Any(parameter => parameter.RefKind != RefKind.None)
            || !SymbolEqualityComparer.Default.Equals(method.ReturnType, method.TypeParameters[0])
            || !SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, method.TypeParameters[0])
            || !IsNativeType(compilation, model.GetTypeInfo(invocation, cancellationToken).Type, "Microsoft.AspNetCore.Routing.RouteGroupBuilder", "Microsoft.AspNetCore.Routing")
            || !IsKnownNativeConvention(compilation, method))
        {
            return false;
        }

        // These framework methods return their receiver. Their conventions may still change
        // the eventual endpoint; this reads only the namespace's declared MapGroup prefixes.
        if (called.ReducedFrom is not null && invocation.Expression is MemberAccessExpressionSyntax member)
        {
            receiver = member.Expression;
        }
        else if (model.GetOperation(invocation, cancellationToken) is IInvocationOperation operation)
        {
            receiver = operation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 0)?.Value.Syntax as ExpressionSyntax;
        }

        return receiver is not null;
    }

    private static bool IsKnownNativeConvention(Compilation compilation, IMethodSymbol method)
    {
        var tail = method.Parameters.Length == 2 ? method.Parameters[1].Type : null;
        if (IsNativeType(compilation, method.ContainingType, "Microsoft.AspNetCore.Builder.RateLimiterEndpointConventionBuilderExtensions", "Microsoft.AspNetCore.RateLimiting"))
        {
            return method.Name switch
            {
                "DisableRateLimiting" => method.Arity == 1 && method.Parameters.Length == 1,
                "RequireRateLimiting" => method.Parameters.Length == 2 && (method.Arity == 1 && tail?.SpecialType == SpecialType.System_String
                                                                           || method.Arity == 2 && tail is INamedTypeSymbol { TypeArguments.Length: 1 } policy
                                                                                                && IsNativeType(compilation, policy.OriginalDefinition, "Microsoft.AspNetCore.RateLimiting.IRateLimiterPolicy`1", "Microsoft.AspNetCore.RateLimiting")
                                                                                                && SymbolEqualityComparer.Default.Equals(policy.TypeArguments[0], method.TypeParameters[1])),
                _ => false
            };
        }

        // Only the exact native typed-policy overload above extends the existing arity-one contract.
        if (method.Arity != 1)
        {
            return false;
        }

        if (IsNativeType(compilation, method.ContainingType, "Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions", "Microsoft.AspNetCore.Routing"))
        {
            return method.Parameters.Length == 2 && (method.Name switch
            {
                "WithName" or "WithGroupName" => tail?.SpecialType == SpecialType.System_String,
                "WithMetadata" => IsArrayOf(tail, SpecialType.System_Object),
                "RequireHost" => IsArrayOf(tail, SpecialType.System_String),
                "WithDisplayName" => tail?.SpecialType == SpecialType.System_String || IsDisplayNameFactory(compilation, tail),
                _ => false
            });
        }

        if (IsNativeType(compilation, method.ContainingType, "Microsoft.AspNetCore.Http.OpenApiRouteHandlerBuilderExtensions", "Microsoft.AspNetCore.Routing"))
        {
            return method.Name switch
            {
                "ExcludeFromDescription" => method.Parameters.Length == 1,
                "WithTags" => method.Parameters.Length == 2 && IsArrayOf(tail, SpecialType.System_String),
                "WithSummary" or "WithDescription" => method.Parameters.Length == 2 && tail?.SpecialType == SpecialType.System_String,
                _ => false
            };
        }

        if (IsNativeType(compilation, method.ContainingType, "Microsoft.AspNetCore.Builder.AuthorizationEndpointConventionBuilderExtensions", "Microsoft.AspNetCore.Authorization.Policy"))
        {
            if (method.Name == "AllowAnonymous")
            {
                return method.Parameters.Length == 1;
            }

            if (method.Name != "RequireAuthorization")
            {
                return false;
            }

            return method.Parameters.Length == 1 || method.Parameters.Length == 2 && (IsArrayOf(tail, SpecialType.System_String)
                                                                                      || tail is IArrayTypeSymbol { Rank: 1 } array && IsNativeType(compilation, array.ElementType, "Microsoft.AspNetCore.Authorization.IAuthorizeData", "Microsoft.AspNetCore.Metadata")
                                                                                      || IsNativeType(compilation, tail, "Microsoft.AspNetCore.Authorization.AuthorizationPolicy", "Microsoft.AspNetCore.Authorization")
                                                                                      || IsActionOf(compilation, tail, "Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder", "Microsoft.AspNetCore.Authorization"));
        }

        if (IsNativeType(compilation, method.ContainingType, "Microsoft.AspNetCore.Builder.CorsEndpointConventionBuilderExtensions", "Microsoft.AspNetCore.Cors"))
        {
            return method.Name == "RequireCors" && (method.Parameters.Length == 1 || method.Parameters.Length == 2
                && (tail?.SpecialType == SpecialType.System_String || IsActionOf(compilation, tail, "Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder", "Microsoft.AspNetCore.Cors")));
        }

        return false;
    }

    private static bool IsNativeType(Compilation compilation, ITypeSymbol? candidate, string metadataName, string assemblyName)
    {
        var nativeType = compilation.GetTypeByMetadataName(metadataName);
        return nativeType is not null && nativeType.DeclaringSyntaxReferences.Length == 0 && nativeType.ContainingAssembly.Name == assemblyName
               && SymbolEqualityComparer.Default.Equals(candidate, nativeType);
    }

    private static bool IsArrayOf(ITypeSymbol? candidate, SpecialType elementType) => candidate is IArrayTypeSymbol { Rank: 1 } array && array.ElementType.SpecialType == elementType;

    private static bool IsActionOf(Compilation compilation, ITypeSymbol? candidate, string argumentMetadataName, string argumentAssemblyName)
    {
        return candidate is INamedTypeSymbol { TypeArguments.Length: 1 } action
               && SymbolEqualityComparer.Default.Equals(action.OriginalDefinition, compilation.GetTypeByMetadataName("System.Action`1"))
               && IsNativeType(compilation, action.TypeArguments[0], argumentMetadataName, argumentAssemblyName);
    }

    private static bool IsDisplayNameFactory(Compilation compilation, ITypeSymbol? candidate)
    {
        return candidate is INamedTypeSymbol { TypeArguments.Length: 2 } factory
               && SymbolEqualityComparer.Default.Equals(factory.OriginalDefinition, compilation.GetTypeByMetadataName("System.Func`2"))
               && IsNativeType(compilation, factory.TypeArguments[0], "Microsoft.AspNetCore.Builder.EndpointBuilder", "Microsoft.AspNetCore.Http.Abstractions")
               && factory.TypeArguments[1].SpecialType == SpecialType.System_String;
    }
}