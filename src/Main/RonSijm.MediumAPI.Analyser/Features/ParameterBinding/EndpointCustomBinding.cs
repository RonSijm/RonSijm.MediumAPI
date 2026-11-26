using Microsoft.CodeAnalysis;

namespace RonSijm.MediumAPI.Analyser;

/// <summary>Recognizes a conservative subset of native custom binding shapes from symbols without executing binding code.</summary>
internal static class EndpointCustomBinding
{
    internal static bool HasContextBinder(Compilation compilation, ITypeSymbol parameterType)
    {
        var type = UnwrapNullable(parameterType) as INamedTypeSymbol;
        if (!IsConcreteBindingType(type))
        {
            return false;
        }

        if (type!.TypeKind == TypeKind.Class && HasSelfInterface(compilation, type, "Microsoft.AspNetCore.Http.IBindableFromHttpContext`1"))
        {
            return true;
        }

        if (HasInterfaceCandidate(type, "BindAsync"))
        {
            return false;
        }

        var context = GetNativeType(compilation, "Microsoft.AspNetCore.Http.HttpContext");
        var parameterInfo = GetNativeType(compilation, "System.Reflection.ParameterInfo");
        if (context is null || parameterInfo is null)
        {
            return false;
        }

        var preferred = FindClassMethod(compilation, type, "BindAsync", null, context, parameterInfo);
        if (preferred.HasCandidates)
        {
            return preferred.Method is not null && HasBinderReturnType(compilation, preferred.Method.ReturnType, type);
        }

        var fallback = FindClassMethod(compilation, type, "BindAsync", null, context);
        return fallback.Method is not null && HasBinderReturnType(compilation, fallback.Method.ReturnType, type);
    }

    internal static bool HasCustomStringParser(Compilation compilation, ITypeSymbol parameterType)
    {
        var type = UnwrapNullable(parameterType) as INamedTypeSymbol;
        if (!IsConcreteBindingType(type) || type!.SpecialType != SpecialType.None || type.TypeKind == TypeKind.Enum
            || IsType(compilation, type, "System.Guid") || IsType(compilation, type, "System.DateTime"))
        {
            return false;
        }

        if (IsType(compilation, type, "System.Uri") || HasSelfInterface(compilation, type, "System.IParsable`1"))
        {
            return true;
        }

        if (HasInterfaceCandidate(type, "TryParse"))
        {
            return false;
        }

        var text = compilation.GetSpecialType(SpecialType.System_String);
        var provider = GetNativeType(compilation, "System.IFormatProvider");
        if (provider is null)
        {
            return false;
        }

        var preferred = FindClassMethod(compilation, type, "TryParse", type, text, provider);
        if (preferred.HasCandidates)
        {
            return preferred.Method is not null && IsParser(preferred.Method);
        }

        var fallback = FindClassMethod(compilation, type, "TryParse", type, text);
        return fallback.Method is not null && IsParser(fallback.Method);
    }

    private static bool IsConcreteBindingType(INamedTypeSymbol? type) => type is not null && !type.IsAbstract && !type.IsRefLikeType && type.TypeKind is TypeKind.Class or TypeKind.Struct;

    private static bool HasSelfInterface(Compilation compilation, INamedTypeSymbol type, string metadataName)
    {
        var contract = GetNativeType(compilation, metadataName);
        return contract is not null && type.AllInterfaces.Any(iface => SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, contract)
                                                                       && iface.TypeArguments.Length == 1 && SymbolEqualityComparer.Default.Equals(iface.TypeArguments[0], type));
    }

    private static bool HasInterfaceCandidate(INamedTypeSymbol type, string methodName) => type.AllInterfaces.Any(iface => iface.GetMembers(methodName).OfType<IMethodSymbol>()
        .Any(method => method.IsStatic && !method.IsAbstract && method.DeclaredAccessibility == Accessibility.Public));

    private static (bool HasCandidates, IMethodSymbol? Method) FindClassMethod(Compilation compilation, INamedTypeSymbol type, string name, ITypeSymbol? outType, params ITypeSymbol[] parameterTypes)
    {
        IMethodSymbol? selected = null;
        var hasCandidates = false;
        var unsupported = false;
        // Search a preferred arity across the entire constructed hierarchy before its
        // fallback. BaseType retains substitutions such as ParserBase<ProductKey>.
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            var candidates = current.GetMembers(name).OfType<IMethodSymbol>()
                .Where(method => method.IsStatic && method.DeclaredAccessibility == Accessibility.Public
                                                 && method.Parameters.Length == parameterTypes.Length + (outType is null ? 0 : 1)
                                                 && CouldMatchParameterTypes(compilation, method, outType, parameterTypes)).ToArray();
            hasCandidates |= candidates.Length != 0;
            var exact = candidates.Where(method => MatchesParameterTypes(method, outType, parameterTypes)).ToArray();
            // Generic/unsupported matching declarations can make native reflection
            // ambiguous even beside an ordinary declaration at another hierarchy level.
            unsupported |= exact.Any(method => !IsOrdinaryStaticMethod(method));
            if (selected is null && exact.Length != 0)
            {
                unsupported |= exact.Length != 1;
                selected = exact[0];
            }
        }

        // Native lookup can match assignable, non-exact parameters. Do not infer a
        // lower-arity fallback merely because an unsupported preferred shape exists.
        if (unsupported || (outType is not null && selected?.Parameters.Last().RefKind != RefKind.Out))
        {
            selected = null;
        }

        return (hasCandidates, selected);
    }

    private static bool CouldMatchParameterTypes(Compilation compilation, IMethodSymbol method, ITypeSymbol? outType, ITypeSymbol[] parameterTypes) => method.Parameters.Take(parameterTypes.Length)
                                                                                                                                                           .Select((parameter, index) => parameter.RefKind == RefKind.None && CouldAcceptNativeArgument(compilation, parameter.Type, parameterTypes[index])).All(matches => matches)
                                                                                                                                                       && (outType is null || (method.Parameters.Last().RefKind != RefKind.None && CouldAcceptNativeArgument(compilation, method.Parameters.Last().Type, outType)));

    private static bool CouldAcceptNativeArgument(Compilation compilation, ITypeSymbol declaredType, ITypeSymbol nativeType)
    {
        if (SymbolEqualityComparer.Default.Equals(declaredType, nativeType) || declaredType.TypeKind is TypeKind.TypeParameter or TypeKind.Error or TypeKind.Dynamic)
        {
            return true;
        }
        // Native lookup can select reference-assignable shapes such as object or
        // ICustomAttributeProvider. User conversions and unrelated overloads do not participate.
        var conversion = compilation.ClassifyCommonConversion(nativeType, declaredType);
        return conversion.IsImplicit && conversion.IsReference && !conversion.IsUserDefined;
    }

    private static bool MatchesParameterTypes(IMethodSymbol method, ITypeSymbol? outType, ITypeSymbol[] parameterTypes) => method.Parameters.Take(parameterTypes.Length)
                                                                                                                               .Select((parameter, index) => parameter.RefKind == RefKind.None && SymbolEqualityComparer.Default.Equals(parameter.Type, parameterTypes[index])).All(matches => matches)
                                                                                                                           && (outType is null || (method.Parameters.Last().RefKind != RefKind.None && SymbolEqualityComparer.Default.Equals(method.Parameters.Last().Type, outType)));

    private static bool IsOrdinaryStaticMethod(IMethodSymbol method) => method.MethodKind == MethodKind.Ordinary && method.IsStatic && method.DeclaredAccessibility == Accessibility.Public && !method.IsAbstract && !method.IsGenericMethod && !method.IsVararg && !method.ReturnsByRef && !method.ReturnsByRefReadonly;

    private static bool IsParser(IMethodSymbol method) => IsOrdinaryStaticMethod(method) && method.ReturnType.SpecialType == SpecialType.System_Boolean;

    private static bool HasBinderReturnType(Compilation compilation, ITypeSymbol returnType, INamedTypeSymbol type) => returnType is INamedTypeSymbol result
                                                                                                                       && SymbolEqualityComparer.Default.Equals(result.OriginalDefinition, GetNativeType(compilation, "System.Threading.Tasks.ValueTask`1"))
                                                                                                                       && result.TypeArguments.Length == 1 && SymbolEqualityComparer.Default.Equals(UnwrapNullable(result.TypeArguments[0]), type);

    private static bool IsType(Compilation compilation, ITypeSymbol type, string metadataName) => SymbolEqualityComparer.Default.Equals(type, GetNativeType(compilation, metadataName));

    private static INamedTypeSymbol? GetNativeType(Compilation compilation, string metadataName)
    {
        var type = compilation.GetTypeByMetadataName(metadataName);
        // A source declaration can shadow a framework full name with only a compiler
        // warning. Its CLR type must not stand in for the native binding contract.
        return type is not null && type.DeclaringSyntaxReferences.Length == 0 && !SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly) ? type : null;
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type) => type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments.Length: 1 } nullable ? nullable.TypeArguments[0] : type;
}