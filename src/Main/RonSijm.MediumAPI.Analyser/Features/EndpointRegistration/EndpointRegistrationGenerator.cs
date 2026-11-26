using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RonSijm.MediumAPI.Analyser;

internal sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo From(SyntaxNode node) => From(node.GetLocation());

    public static LocationInfo From(SyntaxToken token) => From(token.GetLocation());

    private static LocationInfo From(Location location)
    {
        var lineSpan = location.GetLineSpan();
        return new LocationInfo(lineSpan.Path, location.SourceSpan, lineSpan.Span);
    }
}

internal sealed record DiagnosticInfo(string Id, LocationInfo? Location, EquatableArray<string> MessageArgs);

internal readonly struct EquatableArray<T>(ImmutableArray<T> values) : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> values = values;

    public ImmutableArray<T> Values => values.IsDefault ? ImmutableArray<T>.Empty : values;

    public bool Equals(EquatableArray<T> other) => Values.SequenceEqual(other.Values);

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var value in Values)
        {
            hash = unchecked(hash * 31 + (value?.GetHashCode() ?? 0));
        }

        return hash;
    }

    public static EquatableArray<T> Empty => new(ImmutableArray<T>.Empty);
}

/// <summary>
/// Discovers endpoint and namespace declarations at compile time and emits a module initializer
/// that registers their mapping descriptors with the runtime registry.
/// </summary>
[Generator]
public sealed class EndpointRegistrationGenerator : IIncrementalGenerator
{
    internal static readonly DiagnosticDescriptor DuplicateNamespaceGroupDescriptor = new(
        id: EndpointDiagnosticIds.DuplicateNamespaceGroup,
        title: "Namespace has multiple endpoint group definitions",
        messageFormat: "Namespace '{0}' contains multiple INamespaceEndpointGroup definitions. Keep one definition per namespace per assembly.",
        category: "EndpointRouting",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor EndpointMetadataUnparseableDescriptor = new(
        id: EndpointDiagnosticIds.EndpointMetadataUnparseable,
        title: "Endpoint metadata cannot be read by the source generator",
        messageFormat: "Endpoint '{0}' will be skipped: {1}. Use expression-bodied compile-time constant string metadata and a direct 'HttpVerb.X' or verb interface.",
        category: "EndpointRouting",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor InvalidEndpointHandlerDescriptor = CreateDeclarationDescriptor(EndpointDiagnosticIds.InvalidEndpointHandler, "Unsupported endpoint handler declaration", "Endpoint '{0}' will not be registered: {1}. Declare one public, non-generic instance HandleAsync method with by-value parameters.");
    internal static readonly DiagnosticDescriptor InvalidEndpointConfigureDescriptor = CreateDeclarationDescriptor(EndpointDiagnosticIds.InvalidEndpointConfigure, "Invalid endpoint configuration declaration", "Endpoint '{0}' will not be registered: {1}. Use public static void Configure(RouteHandlerBuilder endpoint).");
    internal static readonly DiagnosticDescriptor InvalidEndpointDeclarationDescriptor = CreateDeclarationDescriptor(EndpointDiagnosticIds.InvalidEndpointDeclaration, "Endpoint cannot be registered", "Endpoint '{0}' will not be registered: {1}.");
    internal static readonly DiagnosticDescriptor InvalidNamespaceGroupDeclarationDescriptor = CreateDeclarationDescriptor(EndpointDiagnosticIds.InvalidNamespaceGroupDeclaration, "Namespace group cannot be registered", "Namespace group '{0}' will not be registered: {1}.");
    internal static readonly DiagnosticDescriptor InvalidEndpointPatternDeclarationDescriptor = CreateDeclarationDescriptor(EndpointDiagnosticIds.InvalidEndpointPatternDeclaration, "Endpoint pattern cannot be generated", "Endpoint '{0}' will not be registered: {1}.");

    private static DiagnosticDescriptor CreateDeclarationDescriptor(string id, string title, string message) => new(id, title, message, "EndpointRouting", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var rootNamespace = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
            {
                provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns);
                return ns ?? "MediumApi";
            });

        var endpoints = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, cancellationToken) => GetEndpointInfo(ctx, cancellationToken))
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);

        var groups = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, cancellationToken) => GetNamespaceGroupInfo(ctx, cancellationToken))
            .Where(static result => result is not null).Select(static (result, _) => result!);
        var combined = endpoints.Collect().Combine(groups.Collect()).Combine(rootNamespace);

        context.RegisterSourceOutput(
            combined,
            static (ctx, pair) => Emit(ctx, pair.Left.Left, pair.Left.Right, pair.Right));
    }

    private static bool IsCandidateClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDecl &&
               !classDecl.Modifiers.Any(SyntaxKind.AbstractKeyword) &&
               !classDecl.Modifiers.Any(SyntaxKind.StaticKeyword);
    }

    private sealed record EndpointInfo(
        string FullyQualifiedTypeName,
        string PatternValue,
        string VerbName,
        string? AuthPolicy,
        string? RegistrationGroup,
        bool MapAutomatically,
        bool HasConfigure,
        LocationInfo Location,
        string? GeneratedPatternSource = null,
        string? GeneratedPatternHintName = null);

    private sealed record EndpointParseResult(EndpointInfo? Info, DiagnosticInfo? Diagnostic);

    private sealed record NamespaceGroupInfo(string FullyQualifiedTypeName, string Namespace, LocationInfo Location, DiagnosticInfo? Diagnostic = null, string? GeneratedMapSource = null, string? GeneratedMapHintName = null);

    private static NamespaceGroupInfo? GetNamespaceGroupInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var declaration = (TypeDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var routeGroup = EndpointNamespaceRouteGroup.Read(context.SemanticModel.Compilation, symbol, cancellationToken);
        if (!routeGroup.HasAttribute && (declaration is not ClassDeclarationSyntax || symbol.IsAbstract || symbol.IsStatic || !symbol.AllInterfaces.Any(iface => iface.ToDisplayString() == "RonSijm.MediumAPI.INamespaceEndpointGroup")))
        {
            return null;
        }

        if (!IsPrimaryDeclaration(symbol, declaration))
        {
            return null;
        }

        var declarationProblem = routeGroup.HasAttribute ? routeGroup.Problem : GetDeclarationProblem(symbol);
        var mapMethods = GetMethodsInHierarchy(symbol, "Map").ToArray();
        if (!routeGroup.HasAttribute && declarationProblem is null && (mapMethods.Length != 1 || !IsGroupMethod(mapMethods[0], "Microsoft.AspNetCore.Routing.RouteGroupBuilder", "Microsoft.AspNetCore.Routing.IEndpointRouteBuilder")))
        {
            declarationProblem = "declare one public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) method";
        }

        var conditions = GetMethodsInHierarchy(symbol, "ShouldMap").ToArray();
        if (declarationProblem is null && (conditions.Length > 1 || conditions.Any(method => !IsGroupMethod(method, "bool", "RonSijm.MediumAPI.EndpointMappingContext"))))
        {
            declarationProblem = "ShouldMap must be public static bool ShouldMap(EndpointMappingContext context) when declared";
        }

        return new(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            symbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : symbol.ContainingNamespace.ToDisplayString(),
            LocationInfo.From(declaration.Identifier), declarationProblem is null ? null : CreateDiagnostic(EndpointDiagnosticIds.InvalidNamespaceGroupDeclaration, symbol, routeGroup.Location ?? LocationInfo.From(declaration.Identifier), declarationProblem),
            routeGroup.HasAttribute && declarationProblem is null ? EndpointNamespaceRouteGroup.GenerateMapSource(symbol, routeGroup.Prefix!) : null,
            routeGroup.HasAttribute && declarationProblem is null ? EndpointNamespaceRouteGroup.GetHintName(symbol) : null);
    }

    private static EndpointParseResult? GetEndpointInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var classDecl = (TypeDeclarationSyntax)context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var declaredPattern = EndpointDeclaredPattern.Read(context.SemanticModel.Compilation, symbol, cancellationToken);
        if (!declaredPattern.HasAttribute && (!IsCandidateClass(classDecl) || symbol.IsAbstract || symbol.IsStatic))
        {
            return null;
        }

        if (!declaredPattern.HasAttribute && !symbol.AllInterfaces.Any(i => i.ToDisplayString() == "RonSijm.MediumAPI.IEndpoint"))
        {
            return null;
        }

        if (!IsPrimaryDeclaration(symbol, classDecl))
        {
            return null;
        }

        var classLocation = LocationInfo.From(classDecl.Identifier);
        if (declaredPattern.HasAttribute && declaredPattern.Problem is not null)
        {
            return InvalidResult(EndpointDiagnosticIds.InvalidEndpointPatternDeclaration, symbol, declaredPattern.Location ?? classLocation, declaredPattern.Problem);
        }

        var declarationProblem = GetDeclarationProblem(symbol);
        if (declarationProblem is not null)
        {
            return InvalidResult(EndpointDiagnosticIds.InvalidEndpointDeclaration, symbol, classLocation, declarationProblem);
        }

        var handlers = GetHandlerMethods(symbol);
        if (handlers.Length == 0)
        {
            return new(Info: null, Diagnostic: null); // ENDPOINT007 is owned by the route analyzer.
        }

        if (handlers.Length != 1)
        {
            return InvalidResult(EndpointDiagnosticIds.InvalidEndpointHandler, symbol, classLocation, "HandleAsync is overloaded; reflection cannot choose one handler unambiguously");
        }

        var handler = handlers[0];
        if (handler.DeclaredAccessibility != Accessibility.Public || handler.IsStatic || handler.IsGenericMethod || handler.IsAbstract || handler.IsVararg
            || handler.MethodKind != MethodKind.Ordinary || handler.ReturnsByRef || handler.ReturnsByRefReadonly
            || handler.Parameters.Any(parameter => parameter.RefKind != RefKind.None || IsUnsupportedHandlerType(parameter.Type))
            || IsUnsupportedHandlerType(handler.ReturnType) || (handler.IsAsync && handler.ReturnsVoid))
        {
            return InvalidResult(EndpointDiagnosticIds.InvalidEndpointHandler, symbol, GetMemberLocation(handler, classLocation), "HandleAsync must be public, concrete, non-generic and non-static; ref/out/ref-like, pointer, function-pointer, vararg signatures and async void are unsupported");
        }

        var configurations = GetMethodsInHierarchy(symbol, "Configure").ToArray();
        if (configurations.Length > 1 || configurations.Any(method => method.DeclaredAccessibility != Accessibility.Public || !method.IsStatic
                                                                                                                           || !method.ReturnsVoid || method.IsAsync || method.IsGenericMethod || method.IsVararg || method.Parameters.Length != 1
                                                                                                                           || method.Parameters[0].RefKind != RefKind.None
                                                                                                                           || method.Parameters[0].Type.ToDisplayString() != "Microsoft.AspNetCore.Builder.RouteHandlerBuilder"))
        {
            return InvalidResult(EndpointDiagnosticIds.InvalidEndpointConfigure, symbol, configurations.Length == 0 ? classLocation : GetMemberLocation(configurations[0], classLocation), "Configure must be a single synchronous public static void method accepting RouteHandlerBuilder");
        }

        var patternProperty = GetPropertyDeclaration(symbol, "Pattern");
        var inheritedPattern = declaredPattern.HasAttribute ? null : EndpointDeclaredPattern.ReadInherited(context.SemanticModel.Compilation, symbol, cancellationToken);
        var patternValue = declaredPattern.Pattern ?? inheritedPattern?.Pattern;
        if (!declaredPattern.HasAttribute && inheritedPattern is null && (!EndpointConstantValues.TryGetString(context.SemanticModel.Compilation, patternProperty?.ExpressionBody?.Expression, allowNull: false, out patternValue) || patternValue is null))
        {
            return UnparseableResult(symbol, classLocation, "the 'Pattern' property is missing or is not an expression-bodied non-null compile-time constant string");
        }

        // The 'Verb' property is usually not declared on the class itself anymore - it comes from a
        // default interface implementation on one of the per-verb marker interfaces (IGetEndpoint,
        // IPostEndpoint, etc.). Only fall back to reading an explicit 'Verb' property from the class
        // when it isn't implementing one of those marker interfaces (or wants to override it).
        var verbProperty = GetPropertySymbol(symbol, "Verb");
        var verbMemberName = verbProperty is null ? InferVerbFromInterfaces(context.SemanticModel.Compilation, symbol) : GetVerbMemberName(context.SemanticModel.Compilation, symbol);
        if (verbMemberName is null || !VerbMarkerInterfaceNames.ContainsValue(verbMemberName))
        {
            return UnparseableResult(symbol, classLocation, "the 'Verb' property is missing or is not an expression-bodied 'HttpVerb.X' access, and the class does not implement one of the per-verb endpoint interfaces (IGetEndpoint, IPostEndpoint, etc.)");
        }

        var authPolicy = TryGetAuthPolicy(context, symbol, out var authPolicyUnparseable);

        if (authPolicyUnparseable)
        {
            return UnparseableResult(symbol, classLocation, "the 'AuthorizationPolicy' property is not an expression-bodied compile-time constant string/null or a supported constant-field interpolation");
        }

        var registrationProperty = GetPropertyDeclaration(symbol, "RegistrationGroup");
        string? registrationGroup = null;
        if (GetPropertySymbol(symbol, "RegistrationGroup") is not null
            && !EndpointConstantValues.TryGetString(context.SemanticModel.Compilation, registrationProperty?.ExpressionBody?.Expression, allowNull: true, out registrationGroup))
        {
            return UnparseableResult(symbol, registrationProperty is null ? classLocation : LocationInfo.From(registrationProperty), "RegistrationGroup must be an expression-bodied compile-time constant string or null available in the compilation");
        }

        var info = new EndpointInfo(
            FullyQualifiedTypeName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            PatternValue: patternValue!,
            VerbName: verbMemberName,
            AuthPolicy: authPolicy,
            RegistrationGroup: registrationGroup,
            MapAutomatically: !symbol.AllInterfaces.Any(iface => iface.ToDisplayString() == "RonSijm.MediumAPI.IManuallyMappedEndpoint"),
            HasConfigure: configurations.Length == 1,
            Location: classLocation,
            GeneratedPatternSource: declaredPattern.HasAttribute ? EndpointDeclaredPattern.GeneratePatternSource(symbol, patternValue!) : null,
            GeneratedPatternHintName: declaredPattern.HasAttribute ? EndpointDeclaredPattern.GetHintName(symbol) : null);

        return new EndpointParseResult(info, Diagnostic: null);
    }

    internal static bool IsPrimaryDeclaration(INamedTypeSymbol symbol, TypeDeclarationSyntax declaration)
    {
        var first = symbol.DeclaringSyntaxReferences.FirstOrDefault();
        return first is not null && first.SyntaxTree == declaration.SyntaxTree && first.Span == declaration.Span;
    }

    internal static PropertyDeclarationSyntax? GetPropertyDeclaration(INamedTypeSymbol symbol, string propertyName)
        => GetPropertySymbol(symbol, propertyName)?.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()).OfType<PropertyDeclarationSyntax>().FirstOrDefault();

    private static IPropertySymbol? GetPropertySymbol(INamedTypeSymbol symbol, string propertyName)
    {
        for (var current = symbol; current is not null; current = current.BaseType)
        {
            var property = current.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(candidate => candidate.Name == propertyName
                                                                                                      || candidate.ExplicitInterfaceImplementations.Any(implementation => implementation.Name == propertyName));
            if (property is not null)
            {
                return property;
            }
        }

        return null;
    }

    internal static IEnumerable<IMethodSymbol> GetMethodsInHierarchy(INamedTypeSymbol symbol, string name)
    {
        for (var current = symbol; current is not null; current = current.BaseType)
        {
            var methods = current.GetMembers(name).OfType<IMethodSymbol>().ToArray();
            if (methods.Length == 0)
            {
                continue;
            }

            foreach (var method in methods)
            {
                yield return method;
            }

            yield break;
        }
    }

    internal static IMethodSymbol[] GetHandlerMethods(INamedTypeSymbol symbol)
    {
        var methods = new List<IMethodSymbol>();
        for (var current = symbol; current is not null; current = current.BaseType)
        {
            foreach (var method in current.GetMembers("HandleAsync").OfType<IMethodSymbol>())
            {
                if (!SymbolEqualityComparer.Default.Equals(current, symbol) && method.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (methods.Any(existing => existing.Arity == method.Arity && existing.Parameters.Length == method.Parameters.Length
                                                                           && existing.Parameters.Select((parameter, index) => parameter.RefKind == method.Parameters[index].RefKind
                                                                                                                               && SymbolEqualityComparer.Default.Equals(parameter.Type, method.Parameters[index].Type)).All(equal => equal)))
                {
                    continue;
                }

                methods.Add(method);
            }
        }

        return methods.ToArray();
    }

    internal static string? GetDeclarationProblem(INamedTypeSymbol symbol)
    {
        for (var current = symbol; current is not null; current = current.ContainingType)
        {
            if (current.Arity != 0)
            {
                return "open generic types and types nested inside generic types are unsupported";
            }

            if (current.IsFileLocal)
            {
                return "file-local types cannot be referenced from a generated source file";
            }

            if (current.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected or Accessibility.ProtectedAndInternal)
            {
                return "the type or a containing type is inaccessible to the generated top-level registration class";
            }
        }

        return null;
    }

    private static LocationInfo GetMemberLocation(ISymbol member, LocationInfo fallback)
        => member.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is { } syntax ? LocationInfo.From(syntax) : fallback;

    private static DiagnosticInfo CreateDiagnostic(string id, INamedTypeSymbol symbol, LocationInfo location, string reason)
        => new(id, location, new EquatableArray<string>(ImmutableArray.Create(symbol.Name, reason)));

    private static EndpointParseResult InvalidResult(string id, INamedTypeSymbol symbol, LocationInfo location, string reason)
        => new(Info: null, Diagnostic: CreateDiagnostic(id, symbol, location, reason));

    internal static bool IsGroupMethod(IMethodSymbol method, string returnType, string parameterType)
    {
        return method.DeclaredAccessibility == Accessibility.Public && method.IsStatic && !method.IsGenericMethod && !method.IsAsync && !method.IsVararg
               && !method.ReturnsByRef && !method.ReturnsByRefReadonly
               && method.ReturnType.ToDisplayString() == returnType && method.Parameters.Length == 1
               && method.Parameters[0].RefKind == RefKind.None && method.Parameters[0].Type.ToDisplayString() == parameterType;
    }

    private static bool IsUnsupportedHandlerType(ITypeSymbol type) => type is IPointerTypeSymbol or IFunctionPointerTypeSymbol or INamedTypeSymbol { IsRefLikeType: true };

    private static EndpointParseResult UnparseableResult(INamedTypeSymbol symbol, LocationInfo location, string reason)
    {
        var diagnostic = new DiagnosticInfo(
            EndpointDiagnosticIds.EndpointMetadataUnparseable,
            location,
            new EquatableArray<string>(ImmutableArray.Create(symbol.Name, reason)));
        return new EndpointParseResult(Info: null, Diagnostic: diagnostic);
    }

    private static string? GetVerbMemberName(Compilation compilation, INamedTypeSymbol symbol)
    {
        var property = GetPropertyDeclaration(symbol, "Verb");
        if (property?.ExpressionBody?.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return null;
        }

        var model = compilation.GetSemanticModel(property.SyntaxTree);
        return model.GetSymbolInfo(memberAccess).Symbol is IFieldSymbol field && field.HasConstantValue
                                                                              && SymbolEqualityComparer.Default.Equals(field.ContainingType, compilation.GetTypeByMetadataName("RonSijm.MediumAPI.HttpVerb"))
            ? field.Name
            : null;
    }

    private static readonly Dictionary<string, string> VerbMarkerInterfaceNames = new(StringComparer.Ordinal)
    {
        ["IGetEndpoint"] = "Get",
        ["IPostEndpoint"] = "Post",
        ["IPutEndpoint"] = "Put",
        ["IPatchEndpoint"] = "Patch",
        ["IDeleteEndpoint"] = "Delete",
        ["IHeadEndpoint"] = "Head",
        ["IOptionsEndpoint"] = "Options",
        ["ITraceEndpoint"] = "Trace",
    };

    /// <summary>
    /// Infers the HTTP verb from one of the per-verb marker interfaces (<c>IGetEndpoint</c>,
    /// <c>IPostEndpoint</c>, etc. - generic or not) when the class doesn't declare an explicit
    /// 'Verb' property of its own.
    /// </summary>
    internal static string? InferVerbFromInterfaces(Compilation compilation, INamedTypeSymbol symbol)
    {
        string? selectedVerb = null;
        foreach (var iface in symbol.AllInterfaces)
        {
            if (iface.ContainingNamespace.ToDisplayString() != "RonSijm.MediumAPI"
                || !VerbMarkerInterfaceNames.TryGetValue(iface.Name, out var verb)
                || !SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, compilation.GetTypeByMetadataName("RonSijm.MediumAPI." + iface.MetadataName)))
            {
                continue;
            }

            if (selectedVerb is not null && selectedVerb != verb)
            {
                return null;
            }

            selectedVerb = verb;
        }

        return selectedVerb;
    }

    private static string? TryGetAuthPolicy(GeneratorSyntaxContext context, INamedTypeSymbol symbol, out bool unparseable)
    {
        unparseable = false;

        var property = GetPropertyDeclaration(symbol, "AuthorizationPolicy");

        if (property is null)
        {
            unparseable = GetPropertySymbol(symbol, "AuthorizationPolicy") is not null;
            return null;
        }

        if (property.ExpressionBody?.Expression is null)
        {
            unparseable = true;
            return null;
        }

        var expr = property.ExpressionBody.Expression;

        if (EndpointConstantValues.TryGetString(context.SemanticModel.Compilation, expr, allowNull: true, out var constantValue))
        {
            return constantValue;
        }

        if (expr is InterpolatedStringExpressionSyntax interpolated)
        {
            var value = TryEvaluateInterpolatedString(context.SemanticModel.Compilation.GetSemanticModel(property.SyntaxTree), interpolated);
            if (value is null)
            {
                unparseable = true;
            }

            return value;
        }

        unparseable = true;
        return null;
    }

    private static string? TryEvaluateInterpolatedString(SemanticModel semanticModel, InterpolatedStringExpressionSyntax interpolated)
    {
        var sb = new StringBuilder();

        foreach (var content in interpolated.Contents)
        {
            if (content is InterpolatedStringTextSyntax text)
            {
                sb.Append(text.TextToken.ValueText);
            }
            else if (content is InterpolationSyntax interpolation)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(interpolation.Expression);
                if (symbolInfo.Symbol is IFieldSymbol field && field.HasConstantValue)
                {
                    if (field.ConstantValue is string constValue)
                    {
                        sb.Append(constValue);
                    }
                    else if (field.ContainingType.TypeKind == TypeKind.Enum)
                    {
                        sb.Append(field.Name);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return sb.ToString();
    }

    private static void Emit(SourceProductionContext context, ImmutableArray<EndpointParseResult> results, ImmutableArray<NamespaceGroupInfo> groupResults, string rootNamespace)
    {
        foreach (var invalidGroup in groupResults.Where(group => group.Diagnostic is not null))
        {
            ReportDiagnostic(context, invalidGroup.Diagnostic!);
        }

        var groups = groupResults.Where(group => group.Diagnostic is null).GroupBy(group => group.FullyQualifiedTypeName, StringComparer.Ordinal)
            .Select(group => group.First()).OrderBy(group => group.FullyQualifiedTypeName, StringComparer.Ordinal).ToArray();
        foreach (var group in groups.Where(group => group.GeneratedMapSource is not null))
        {
            context.AddSource(group.GeneratedMapHintName!, SourceText.From(group.GeneratedMapSource!, Encoding.UTF8));
        }

        foreach (var duplicate in groups.GroupBy(group => group.Namespace, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            foreach (var definition in duplicate)
            {
                context.ReportDiagnostic(Diagnostic.Create(DuplicateNamespaceGroupDescriptor, definition.Location.ToLocation(), duplicate.Key));
            }
        }

        groups = groups.GroupBy(group => group.Namespace, StringComparer.Ordinal).Where(group => group.Count() == 1).Select(group => group.Single()).ToArray();
        foreach (var result in results)
        {
            if (result.Diagnostic is { } diag)
            {
                ReportDiagnostic(context, diag);
            }
        }

        var infos = results
            .Where(r => r.Info is not null)
            .Select(r => r.Info!)
            .GroupBy(info => info.FullyQualifiedTypeName, StringComparer.Ordinal).Select(group => group.First())
            .ToImmutableArray();

        if (infos.IsEmpty && groups.Length == 0)
        {
            return;
        }

        var ordered = infos.OrderBy(i => i.FullyQualifiedTypeName, StringComparer.Ordinal).ToArray();
        foreach (var info in ordered.Where(info => info.GeneratedPatternSource is not null))
        {
            context.AddSource(info.GeneratedPatternHintName!, SourceText.From(info.GeneratedPatternSource!, Encoding.UTF8));
        }

        GenerateEndpointRegistration(context, ordered, groups, rootNamespace);
    }

    private static void ReportDiagnostic(SourceProductionContext context, DiagnosticInfo diagnostic)
    {
        var descriptor = diagnostic.Id switch
        {
            EndpointDiagnosticIds.EndpointMetadataUnparseable => EndpointMetadataUnparseableDescriptor,
            EndpointDiagnosticIds.InvalidEndpointHandler => InvalidEndpointHandlerDescriptor,
            EndpointDiagnosticIds.InvalidEndpointConfigure => InvalidEndpointConfigureDescriptor,
            EndpointDiagnosticIds.InvalidEndpointDeclaration => InvalidEndpointDeclarationDescriptor,
            EndpointDiagnosticIds.InvalidNamespaceGroupDeclaration => InvalidNamespaceGroupDeclarationDescriptor,
            EndpointDiagnosticIds.InvalidEndpointPatternDeclaration => InvalidEndpointPatternDeclarationDescriptor,
            _ => null
        };
        if (descriptor is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, diagnostic.Location?.ToLocation() ?? Location.None, diagnostic.MessageArgs.Values.Cast<object?>().ToArray()));
        }
    }

    /// <summary>
    /// Emits a module initializer that self-registers this project's endpoints into
    /// <c>RonSijm.MediumAPI.EndpointRegistry</c> as soon as the assembly is loaded. This is what lets a host
    /// project map endpoints from every referenced project with a single
    /// <c>app.MapMediumApiEndpoints()</c> call - each project registers itself, nobody has to enumerate or
    /// qualify anything by namespace.
    /// </summary>
    private static void GenerateEndpointRegistration(SourceProductionContext context, EndpointInfo[] infos, NamespaceGroupInfo[] groups, string rootNamespace)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using Microsoft.AspNetCore.Builder;");
        sb.AppendLine("using Microsoft.AspNetCore.Http;");
        sb.AppendLine("using Microsoft.AspNetCore.Routing;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using RonSijm.MediumAPI;");
        sb.AppendLine($"internal static class MediumApiEndpointRegistration_{SanitizeIdentifier(rootNamespace)}");
        sb.AppendLine("{");
        sb.AppendLine("    [System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    internal static void Initialize()");
        sb.AppendLine("    {");
        foreach (var group in groups)
        {
            sb.AppendLine($"        EndpointRegistry.RegisterNamespaceGroup<{group.FullyQualifiedTypeName}>({group.FullyQualifiedTypeName}.Map);");
        }

        foreach (var info in infos)
        {
            var type = info.FullyQualifiedTypeName;
            var pattern = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(info.PatternValue, true);
            var group = info.RegistrationGroup is null ? "null" : Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(info.RegistrationGroup, true);
            sb.AppendLine($"        EndpointRegistry.Register<{type}>((builder, patternOverride) =>");
            sb.AppendLine("        {");
            sb.AppendLine($"            var mapping = EndpointHandlerAdapter.Map<{type}>(builder, patternOverride ?? {pattern}, \"{info.VerbName.ToUpperInvariant()}\");");
            if (info.AuthPolicy is not null)
            {
                sb.AppendLine($"            mapping.RequireAuthorization({Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(info.AuthPolicy, true)});");
            }

            if (info.HasConfigure)
            {
                sb.AppendLine($"            {type}.Configure(mapping);");
            }

            sb.AppendLine("            return mapping;");
            sb.AppendLine($"        }}, {group}, mapAutomatically: {(info.MapAutomatically ? "true" : "false")}, pattern: {pattern}, httpMethod: \"{info.VerbName.ToUpperInvariant()}\");");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        context.AddSource("MediumApiEndpointRegistration.g.cs", sb.ToString());
    }

    private static string SanitizeIdentifier(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        return sb.ToString();
    }
}