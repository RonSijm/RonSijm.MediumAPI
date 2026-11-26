using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RonSijm.MediumAPI.Analyser;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EndpointRouteAnalyzer : DiagnosticAnalyzer
{
    private static DiagnosticDescriptor CreateDescriptor(string id, string title, string messageFormat, DiagnosticSeverity severity) =>
        new(
            id: id,
            title: title,
            messageFormat: messageFormat,
            category: "EndpointRouting",
            defaultSeverity: severity,
            isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor RouteParameterMissingFromHandler =
        CreateDescriptor(
            EndpointDiagnosticIds.RouteParameterMissingFromHandler,
            "Route parameter is missing from endpoint handler",
            "Route parameter '{0}' does not match any handler parameter",
            DiagnosticSeverity.Error);

    private static readonly DiagnosticDescriptor HandlerParameterMissingFromRoute =
        CreateDescriptor(
            EndpointDiagnosticIds.HandlerParameterMissingFromRoute,
            "Handler parameter is missing from route",
            "Handler parameter '{0}' is not present in the route",
            DiagnosticSeverity.Error);

    private static readonly DiagnosticDescriptor RouteConstraintTypeMismatch =
        CreateDescriptor(
            EndpointDiagnosticIds.RouteConstraintTypeMismatch,
            "Route constraint does not match handler parameter type",
            "Route parameter '{0}' uses constraint '{1}', but handler parameter type is '{2}'",
            DiagnosticSeverity.Error);

    private static readonly DiagnosticDescriptor InheritedRouteParameterMissingFromLayout =
        CreateDescriptor(
            EndpointDiagnosticIds.InheritedRouteParameterMissingFromLayout,
            "Explicit route binding is absent from the declared namespace layout",
            "Explicit route binding '{0}' is absent from the readable namespace-group and endpoint patterns; an outer host group may provide it",
            DiagnosticSeverity.Info);

    private static readonly DiagnosticDescriptor InheritedRouteConstraintTypeMismatch =
        CreateDescriptor(
            EndpointDiagnosticIds.InheritedRouteConstraintTypeMismatch,
            "Inherited route constraint does not match the explicit handler parameter type",
            "Inherited namespace route parameter '{0}' uses constraint '{1}', but explicit handler parameter type is '{2}'",
            DiagnosticSeverity.Info);

    private static readonly DiagnosticDescriptor EndpointLambdaShouldBeStatic =
        CreateDescriptor(
            EndpointDiagnosticIds.EndpointLambdaShouldBeStatic,
            "Endpoint lambda should be static",
            "Endpoint route lambda should be static to avoid accidentally capturing startup state",
            DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor PatternPropertyMissingStringSyntaxAttribute =
        CreateDescriptor(
            EndpointDiagnosticIds.PatternPropertyMissingStringSyntaxAttribute,
            "Pattern property is missing [StringSyntax(\"Route\")] attribute",
            "IEndpoint.Pattern property should be decorated with [StringSyntax(\"Route\")] to enable IDE route tooling",
            DiagnosticSeverity.Warning);

    private static readonly DiagnosticDescriptor HandleAsyncMethodMissing =
        CreateDescriptor(
            EndpointDiagnosticIds.HandleAsyncMethodMissing,
            "IEndpoint implementation is missing a HandleAsync method",
            "Endpoint '{0}' must declare a public HandleAsync method",
            DiagnosticSeverity.Error);

    // Normalize simple terminal optional/default constraints before legacy extraction; keep catch-all guards.
    private static readonly Regex RouteParameterRegex = new(@"(?:^|/)\{(?<name>[a-zA-Z_][a-zA-Z0-9_]*):(?<constraint>[a-zA-Z_][a-zA-Z0-9_]*)(?:\?|=[A-Za-z0-9_.+-]+)\}/?\z|\{(?<name>[a-zA-Z_][a-zA-Z0-9_]*)(:(?<constraint>[^}:]+))?[^}]*\}|(?:^|/)\{\*{1,2}(?<name>[a-zA-Z_][a-zA-Z0-9_]*)(?::(?<constraint>[a-zA-Z_][a-zA-Z0-9_]*))?\}/?\z", RegexOptions.Compiled);

    private static readonly ImmutableHashSet<string> EndpointMethodNames =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "Get",
            "Post",
            "Put",
            "Patch",
            "Delete");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        RouteParameterMissingFromHandler,
        HandlerParameterMissingFromRoute,
        RouteConstraintTypeMismatch,
        EndpointLambdaShouldBeStatic,
        PatternPropertyMissingStringSyntaxAttribute,
        HandleAsyncMethodMissing,
        InheritedRouteParameterMissingFromLayout,
        InheritedRouteConstraintTypeMismatch,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(
            AnalyzeInvocation,
            SyntaxKind.InvocationExpression);

        context.RegisterSyntaxNodeAction(
            AnalyzeClassDeclaration,
            SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var methodName = GetInvokedMethodName(invocation);
        if (methodName is null || !EndpointMethodNames.Contains(methodName))
        {
            return;
        }

        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count < 2)
        {
            return;
        }

        var routeArgument = arguments.FirstOrDefault(IsStringLiteralArgument);
        if (routeArgument is null)
        {
            return;
        }

        var lambdaArgument = arguments.FirstOrDefault(IsLambdaArgument);
        if (lambdaArgument is null)
        {
            return;
        }

        var routePattern = GetStringLiteralValue(routeArgument);
        if (routePattern is null)
        {
            return;
        }

        var lambda = (LambdaExpressionSyntax)lambdaArgument.Expression;

        AnalyzeStaticLambda(context, lambda);
        AnalyzeRouteParameters(context, routeArgument, routePattern, lambda);
    }

    private static string? GetInvokedMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            _ => null
        };
    }

    private static bool IsStringLiteralArgument(ArgumentSyntax argument)
    {
        return argument.Expression is LiteralExpressionSyntax literal &&
               literal.IsKind(SyntaxKind.StringLiteralExpression);
    }

    private static bool IsLambdaArgument(ArgumentSyntax argument)
    {
        return argument.Expression is LambdaExpressionSyntax;
    }

    private static string? GetStringLiteralValue(ArgumentSyntax argument)
    {
        if (argument.Expression is not LiteralExpressionSyntax literal)
        {
            return null;
        }

        return literal.Token.ValueText;
    }

    private static void AnalyzeStaticLambda(SyntaxNodeAnalysisContext context, LambdaExpressionSyntax lambda)
    {
        if (lambda.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                EndpointLambdaShouldBeStatic,
                lambda.GetLocation()));
    }

    private static void AnalyzeRouteParameters(SyntaxNodeAnalysisContext context, ArgumentSyntax routeArgument, string routePattern, LambdaExpressionSyntax lambda)
    {
        var routeParameters = ParseRouteParameters(routePattern);

        var allParameters = GetLambdaParameters(context, lambda);
        var lambdaParameters = allParameters
            .Where(parameter => !ShouldIgnoreParameter(context.Compilation, parameter))
            .ToArray();

        var lambdaParametersByName = lambdaParameters
            .Where(parameter => parameter.Binding.RouteName is not null)
            .GroupBy(parameter => parameter.Binding.RouteName!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var hasOpaqueBinding = allParameters.Any(parameter => HasOpaqueBinding(context.Compilation, parameter));

        foreach (var routeParameter in routeParameters)
        {
            if (!lambdaParametersByName.TryGetValue(routeParameter.Name, out var matchingLambdaParameter))
            {
                if (hasOpaqueBinding)
                {
                    continue;
                }

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        RouteParameterMissingFromHandler,
                        routeArgument.GetLocation(),
                        routeParameter.Name));

                continue;
            }

            if (routeParameter.Constraint is not null)
            {
                AnalyzeConstraint(
                    context,
                    routeArgument,
                    routeParameter,
                    matchingLambdaParameter);
            }
        }

        var routeParameterNames = new HashSet<string>(
            routeParameters.Select(parameter => parameter.Name),
            StringComparer.OrdinalIgnoreCase);

        foreach (var lambdaParameter in lambdaParameters)
        {
            if (!lambdaParameter.Binding.HasExplicitSource && !routeParameterNames.Contains(lambdaParameter.Binding.RouteName!))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        HandlerParameterMissingFromRoute,
                        lambdaParameter.Syntax.GetLocation(),
                        lambdaParameter.Symbol.Name));
            }
        }

        AnalyzeAggregateRouteParameters(context, routeArgument.GetLocation(), routeParameters, allParameters);
    }

    private static RouteParameter[] ParseRouteParameters(string routePattern)
    {
        return RouteParameterRegex
            .Matches(routePattern)
            .Cast<Match>()
            .Select(match => new RouteParameter(
                Name: match.Groups["name"].Value,
                Constraint: match.Groups["constraint"].Success
                    ? match.Groups["constraint"].Value
                    : null))
            .ToArray();
    }

    private static LambdaParameter[] GetLambdaParameters(SyntaxNodeAnalysisContext context, LambdaExpressionSyntax lambda)
    {
        return lambda switch
        {
            ParenthesizedLambdaExpressionSyntax parenthesized =>
                parenthesized.ParameterList.Parameters
                    .Select(parameter => (
                        Syntax: parameter,
                        Symbol: context.SemanticModel.GetDeclaredSymbol(parameter, context.CancellationToken)))
                    .Where(p => p.Symbol is not null)
                    .Select(p => new LambdaParameter(p.Syntax!, p.Symbol!, EndpointParameterBinding.Read(context.Compilation, p.Symbol!)))
                    .ToArray(),

            SimpleLambdaExpressionSyntax simple =>
                context.SemanticModel.GetDeclaredSymbol(simple.Parameter, context.CancellationToken) is { } symbol
                    ? [new LambdaParameter(simple.Parameter, symbol, EndpointParameterBinding.Read(context.Compilation, symbol))]
                    : [],

            _ => []
        };
    }

    private static bool ShouldIgnoreParameter(Compilation compilation, LambdaParameter parameter)
    {
        if (parameter.Binding.Kind is EndpointBindingSourceKind.NonRoute or EndpointBindingSourceKind.Aggregate or EndpointBindingSourceKind.Opaque)
        {
            return true;
        }
        // A route-bound value is a route candidate even when its type has a custom parser.
        if (parameter.Binding.Kind == EndpointBindingSourceKind.Route)
        {
            return false;
        }

        var type = parameter.Symbol.Type;
        if (EndpointCustomBinding.HasContextBinder(compilation, type))
        {
            return true;
        }

        if (EndpointCustomBinding.HasCustomStringParser(compilation, type))
        {
            return false;
        }

        if (IsType(type, "System.Threading.CancellationToken"))
        {
            return true;
        }

        if (IsType(type, "Microsoft.AspNetCore.Http.HttpContext"))
        {
            return true;
        }

        if (IsType(type, "Microsoft.AspNetCore.Http.HttpRequest"))
        {
            return true;
        }

        if (IsType(type, "Microsoft.AspNetCore.Http.HttpResponse"))
        {
            return true;
        }

        if (type.TypeKind == TypeKind.Interface)
        {
            return true;
        }

        // Preserve the existing complex-reference heuristic after recognizing supported
        // custom parsers and binders. It does not infer DI or all native binding rules.
        if (type.TypeKind == TypeKind.Class && type.SpecialType != SpecialType.System_String)
        {
            return true;
        }

        if (parameter.Symbol.Name.Equals("endpoint", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (type.Name.EndsWith("Endpoint", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static bool HasOpaqueBinding(Compilation compilation, LambdaParameter parameter) => parameter.Binding.IsOpaque
                                                                                                || !parameter.Binding.HasExplicitSource && EndpointCustomBinding.HasContextBinder(compilation, parameter.Symbol.Type);

    private static bool HasRouteStringParser(Compilation compilation, LambdaParameter parameter) => (parameter.Binding.Kind is EndpointBindingSourceKind.None or EndpointBindingSourceKind.Route)
                                                                                                    && EndpointCustomBinding.HasCustomStringParser(compilation, parameter.Symbol.Type);

    private static void AnalyzeConstraint(SyntaxNodeAnalysisContext context, ArgumentSyntax routeArgument, RouteParameter routeParameter, LambdaParameter lambdaParameter)
    {
        if (HasRouteStringParser(context.Compilation, lambdaParameter))
        {
            return;
        }

        var expectedTypeName = GetExpectedTypeNameForConstraint(routeParameter.Constraint!);
        if (expectedTypeName is null)
        {
            return;
        }

        var actualType = UnwrapNullable(lambdaParameter.Symbol.Type);

        if (actualType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == expectedTypeName)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                RouteConstraintTypeMismatch,
                routeArgument.GetLocation(),
                routeParameter.Name,
                routeParameter.Constraint,
                actualType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static string? GetExpectedTypeNameForConstraint(string constraint)
    {
        return constraint.ToLowerInvariant() switch
        {
            "guid" => "global::System.Guid",
            "int" => "int",
            "long" => "long",
            "bool" => "bool",
            "decimal" => "decimal",
            "double" => "double",
            "datetime" => "global::System.DateTime",
            _ => null
        };
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            namedType.TypeArguments.Length == 1)
        {
            return namedType.TypeArguments[0];
        }

        return type;
    }

    private static bool IsType(ITypeSymbol type, string fullyQualifiedMetadataName)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Equals("global::" + fullyQualifiedMetadataName, StringComparison.Ordinal);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken);
        if (classSymbol is null)
        {
            return;
        }

        if (!classSymbol.AllInterfaces.Any(i => i.ToDisplayString() == "RonSijm.MediumAPI.IEndpoint"))
        {
            return;
        }

        if (classSymbol.IsAbstract || !EndpointRegistrationGenerator.IsPrimaryDeclaration(classSymbol, classDeclaration))
        {
            return;
        }

        var declaredPattern = EndpointDeclaredPattern.Read(context.Compilation, classSymbol, context.CancellationToken);
        if (declaredPattern.HasAttribute && declaredPattern.Problem is not null)
        {
            return;
        }

        var inheritedPattern = declaredPattern.HasAttribute ? null : EndpointDeclaredPattern.ReadInherited(context.Compilation, classSymbol, context.CancellationToken);
        var sourcePattern = declaredPattern.HasAttribute ? declaredPattern : inheritedPattern;
        string? routePattern;
        Location patternLocation;
        if (sourcePattern is not null)
        {
            routePattern = sourcePattern.Pattern;
            patternLocation = sourcePattern.Location?.ToLocation() ?? classDeclaration.Identifier.GetLocation();
        }
        else
        {
            var patternProperty = EndpointRegistrationGenerator.GetPropertyDeclaration(classSymbol, "Pattern");
            if (patternProperty is null)
            {
                return;
            }

            if (!HasStringSyntaxRouteAttribute(context, patternProperty))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    PatternPropertyMissingStringSyntaxAttribute,
                    patternProperty.Identifier.GetLocation()));
            }

            routePattern = GetPropertyConstantString(context.Compilation, patternProperty);
            patternLocation = patternProperty.GetLocation();
        }

        if (routePattern is null)
        {
            return;
        }

        var handleAsyncMethods = EndpointRegistrationGenerator.GetHandlerMethods(classSymbol);

        if (handleAsyncMethods.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                HandleAsyncMethodMissing,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name));

            return;
        }

        // Declaration errors are reported by the generator; avoid speculative route errors for overloads.
        if (handleAsyncMethods.Length != 1)
        {
            return;
        }

        var handleAsyncSymbol = handleAsyncMethods[0];
        // Referenced handlers have no parameter syntax in this compilation; runtime binding remains authoritative.
        if (handleAsyncSymbol.DeclaringSyntaxReferences.Length == 0)
        {
            return;
        }

        var handlerParameters = GetMethodParameters(context, handleAsyncSymbol);
        AnalyzeRouteParameters(context, patternLocation, routePattern, handlerParameters);
        AnalyzeInheritedRouteParameters(context, classSymbol, routePattern, handlerParameters);
    }

    private static void AnalyzeInheritedRouteParameters(SyntaxNodeAnalysisContext context, INamedTypeSymbol endpointType, string localPattern, LambdaParameter[] handlerParameters)
    {
        if (!EndpointNamespaceRouteLayout.IsReadablePattern(localPattern)
            || !EndpointNamespaceRouteLayout.TryGetPrefixes(context.Compilation, endpointType, context.CancellationToken, out var prefixes))
        {
            return;
        }

        var inheritedParameters = prefixes.SelectMany(ParseRouteParameters).ToArray();
        var declaredParameters = inheritedParameters.Concat(ParseRouteParameters(localPattern)).ToArray();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Duplicate names make the composed layout ambiguous; ordinary local diagnostics still apply.
        if (declaredParameters.Any(parameter => !names.Add(parameter.Name)))
        {
            return;
        }

        foreach (var parameter in handlerParameters)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (parameter.Binding.Kind != EndpointBindingSourceKind.Route || parameter.Binding.RouteName is not { } name)
            {
                continue;
            }

            if (!names.Contains(name))
            {
                context.ReportDiagnostic(Diagnostic.Create(InheritedRouteParameterMissingFromLayout, parameter.Syntax.GetLocation(), name));
                continue;
            }

            var inherited = inheritedParameters.FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
            if (inherited?.Constraint is null)
            {
                continue;
            }

            if (HasRouteStringParser(context.Compilation, parameter))
            {
                continue;
            }

            var expectedTypeName = GetExpectedTypeNameForConstraint(inherited.Constraint);
            var actualType = UnwrapNullable(parameter.Symbol.Type);
            if (expectedTypeName is null || actualType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == expectedTypeName)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(InheritedRouteConstraintTypeMismatch, parameter.Syntax.GetLocation(), name,
                inherited.Constraint, actualType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }

        foreach (var member in GetAggregateRouteMembers(context, handlerParameters))
        {
            if (!names.Contains(member.RouteName))
            {
                context.ReportDiagnostic(Diagnostic.Create(InheritedRouteParameterMissingFromLayout, member.Location, member.RouteName));
                continue;
            }

            var inherited = inheritedParameters.FirstOrDefault(candidate => string.Equals(candidate.Name, member.RouteName, StringComparison.OrdinalIgnoreCase));
            if (inherited?.Constraint is not { } constraint || GetAggregateConstraintMismatch(context.Compilation, member, constraint) is not { } actualType)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(InheritedRouteConstraintTypeMismatch, member.Location, member.RouteName,
                constraint, actualType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }

    private static string? GetPropertyConstantString(Compilation compilation, PropertyDeclarationSyntax property)
    {
        return EndpointConstantValues.TryGetString(compilation, property.ExpressionBody?.Expression, allowNull: false, out var value) ? value : null;
    }

    private static bool HasStringSyntaxRouteAttribute(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax property)
    {
        var model = context.Compilation.GetSemanticModel(property.SyntaxTree);
        var symbol = model.GetDeclaredSymbol(property, context.CancellationToken);
        return symbol?.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == "System.Diagnostics.CodeAnalysis.StringSyntaxAttribute"
            && attribute.ConstructorArguments.Length > 0
            && attribute.ConstructorArguments[0].Value is string syntax
            && string.Equals(syntax, "Route", StringComparison.Ordinal)) == true;
    }

    private static LambdaParameter[] GetMethodParameters(SyntaxNodeAnalysisContext context, IMethodSymbol method)
    {
        return method.Parameters
            .Select(p => (
                Symbol: p,
                Syntax: p.DeclaringSyntaxReferences
                    .FirstOrDefault()
                    ?.GetSyntax(context.CancellationToken) as ParameterSyntax))
            .Where(p => p.Syntax is not null)
            .Select(p => new LambdaParameter(p.Syntax!, p.Symbol, EndpointParameterBinding.Read(context.Compilation, p.Symbol)))
            .ToArray();
    }

    private static void AnalyzeRouteParameters(SyntaxNodeAnalysisContext context, Location patternLocation, string routePattern, LambdaParameter[] handlerParameters)
    {
        var routeParameters = ParseRouteParameters(routePattern);

        var filteredParameters = handlerParameters
            .Where(parameter => !ShouldIgnoreParameter(context.Compilation, parameter))
            .ToArray();

        var parametersByName = filteredParameters.Where(parameter => parameter.Binding.RouteName is not null)
            .GroupBy(parameter => parameter.Binding.RouteName!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var hasOpaqueBinding = handlerParameters.Any(parameter => HasOpaqueBinding(context.Compilation, parameter));

        foreach (var routeParameter in routeParameters)
        {
            if (!parametersByName.TryGetValue(routeParameter.Name, out var matchingParameter))
            {
                // Aggregates, unknown explicit route names and context binders can consume
                // route values. Missing-token guesses cannot prove those values are unused.
                if (hasOpaqueBinding)
                {
                    continue;
                }

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        RouteParameterMissingFromHandler,
                        patternLocation,
                        routeParameter.Name));

                continue;
            }

            if (routeParameter.Constraint is not null)
            {
                AnalyzeConstraint(
                    context,
                    patternLocation,
                    routeParameter,
                    matchingParameter);
            }
        }

        var routeParameterNames = new HashSet<string>(
            routeParameters.Select(parameter => parameter.Name),
            StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in filteredParameters)
        {
            if (!parameter.Binding.HasExplicitSource && !routeParameterNames.Contains(parameter.Binding.RouteName!))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        HandlerParameterMissingFromRoute,
                        parameter.Syntax.GetLocation(),
                        parameter.Symbol.Name));
            }
        }

        AnalyzeAggregateRouteParameters(context, patternLocation, routeParameters, handlerParameters);
    }

    private static EndpointAggregateRouteMember[] GetAggregateRouteMembers(SyntaxNodeAnalysisContext context, LambdaParameter[] parameters)
    {
        // Keep the original aggregate opaque for missing-token checks. Only independent,
        // proven route properties participate in these additional constraint/advisory checks.
        var directNames = new HashSet<string>(parameters.Where(parameter => !ShouldIgnoreParameter(context.Compilation, parameter))
            .Select(parameter => parameter.Binding.RouteName).OfType<string>(), StringComparer.OrdinalIgnoreCase);
        var members = parameters.Where(parameter => parameter.Binding.Kind == EndpointBindingSourceKind.Aggregate)
            .SelectMany(parameter => EndpointAggregateRouteBinding.Read(context.Compilation, parameter.Symbol, context.CancellationToken)).ToArray();
        var counts = members.GroupBy(member => member.RouteName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        return members.Where(member => counts[member.RouteName] == 1 && !directNames.Contains(member.RouteName)).ToArray();
    }

    private static void AnalyzeAggregateRouteParameters(SyntaxNodeAnalysisContext context, Location location, RouteParameter[] routeParameters, LambdaParameter[] parameters)
    {
        foreach (var member in GetAggregateRouteMembers(context, parameters))
        {
            var matching = routeParameters.Where(parameter => string.Equals(parameter.Name, member.RouteName, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matching.Length != 1 || matching[0].Constraint is not { } constraint
                                     || GetAggregateConstraintMismatch(context.Compilation, member, constraint) is not { } actualType)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(RouteConstraintTypeMismatch, location, matching[0].Name, constraint,
                actualType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }

    private static ITypeSymbol? GetAggregateConstraintMismatch(Compilation compilation, EndpointAggregateRouteMember member, string constraint)
    {
        if (EndpointCustomBinding.HasCustomStringParser(compilation, member.Symbol.Type))
        {
            return null;
        }

        var expectedTypeName = GetExpectedTypeNameForConstraint(constraint);
        var actualType = UnwrapNullable(member.Symbol.Type);
        return expectedTypeName is null || actualType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == expectedTypeName ? null : actualType;
    }

    private static void AnalyzeConstraint(SyntaxNodeAnalysisContext context, Location location, RouteParameter routeParameter, LambdaParameter lambdaParameter)
    {
        if (HasRouteStringParser(context.Compilation, lambdaParameter))
        {
            return;
        }

        var expectedTypeName = GetExpectedTypeNameForConstraint(routeParameter.Constraint!);
        if (expectedTypeName is null)
        {
            return;
        }

        var actualType = UnwrapNullable(lambdaParameter.Symbol.Type);

        if (actualType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == expectedTypeName)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                RouteConstraintTypeMismatch,
                location,
                routeParameter.Name,
                routeParameter.Constraint,
                actualType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private sealed record RouteParameter(string Name, string? Constraint);

    private sealed record LambdaParameter(ParameterSyntax Syntax, IParameterSymbol Symbol, EndpointParameterBinding Binding);
}