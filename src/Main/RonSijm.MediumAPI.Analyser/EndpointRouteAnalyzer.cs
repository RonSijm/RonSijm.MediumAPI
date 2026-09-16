using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace RonSijm.MediumAPI.Analyser;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EndpointRouteAnalyzer : DiagnosticAnalyzer
{
	private static DiagnosticDescriptor CreateDescriptor(
		string id,
		string title,
		string messageFormat,
		DiagnosticSeverity severity) =>
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
			"IEndpointAdapter.Pattern property should be decorated with [StringSyntax(\"Route\")] to enable IDE route tooling",
			DiagnosticSeverity.Warning);

	private static readonly DiagnosticDescriptor HandleAsyncMethodMissing =
		CreateDescriptor(
			EndpointDiagnosticIds.HandleAsyncMethodMissing,
			"IEndpointAdapter implementation is missing a HandleAsync method",
			"Endpoint '{0}' must declare a public HandleAsync method",
			DiagnosticSeverity.Error);

	private static readonly Regex RouteParameterRegex = new(@"\{(?<name>[a-zA-Z_][a-zA-Z0-9_]*)(:(?<constraint>[^}:]+))?[^}]*\}", RegexOptions.Compiled);

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

	private static void AnalyzeStaticLambda(
		SyntaxNodeAnalysisContext context,
		LambdaExpressionSyntax lambda)
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

	private static void AnalyzeRouteParameters(
		SyntaxNodeAnalysisContext context,
		ArgumentSyntax routeArgument,
		string routePattern,
		LambdaExpressionSyntax lambda)
	{
		var routeParameters = ParseRouteParameters(routePattern);

		var lambdaParameters = GetLambdaParameters(context, lambda)
			.Where(parameter => !ShouldIgnoreParameter(parameter.Symbol))
			.ToArray();

		var lambdaParametersByName = lambdaParameters
			.ToDictionary(
				parameter => parameter.Symbol.Name,
				parameter => parameter,
				StringComparer.OrdinalIgnoreCase);

		foreach (var routeParameter in routeParameters)
		{
			if (!lambdaParametersByName.TryGetValue(routeParameter.Name, out var matchingLambdaParameter))
			{
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
					matchingLambdaParameter.Symbol);
			}
		}

		var routeParameterNames = new HashSet<string>(
			routeParameters.Select(parameter => parameter.Name),
			StringComparer.OrdinalIgnoreCase);

		foreach (var lambdaParameter in lambdaParameters)
		{
			if (!routeParameterNames.Contains(lambdaParameter.Symbol.Name))
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						HandlerParameterMissingFromRoute,
						lambdaParameter.Syntax.GetLocation(),
						lambdaParameter.Symbol.Name));
			}
		}
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
					.Select(p => new LambdaParameter(p.Syntax!, p.Symbol!))
					.ToArray(),

			SimpleLambdaExpressionSyntax simple =>
				context.SemanticModel.GetDeclaredSymbol(simple.Parameter, context.CancellationToken) is { } symbol
					? [new LambdaParameter(simple.Parameter, symbol)]
					: [],

			_ => []
		};
	}

	private static bool ShouldIgnoreParameter(IParameterSymbol parameter)
	{
		var type = parameter.Type;

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

		if (parameter.Name.Equals("endpoint", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (type.Name.EndsWith("Endpoint", StringComparison.Ordinal))
		{
			return true;
		}

		return false;
	}

	private static void AnalyzeConstraint(
		SyntaxNodeAnalysisContext context,
		ArgumentSyntax routeArgument,
		RouteParameter routeParameter,
		IParameterSymbol lambdaParameter)
	{
		var expectedTypeName = GetExpectedTypeNameForConstraint(routeParameter.Constraint!);
		if (expectedTypeName is null)
		{
			return;
		}

		var actualType = UnwrapNullable(lambdaParameter.Type);

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

		if (!classSymbol.AllInterfaces.Any(i => i.Name == "IEndpointAdapter"))
		{
			return;
		}

		var patternProperty = classDeclaration.Members
			.OfType<PropertyDeclarationSyntax>()
			.FirstOrDefault(p => p.Identifier.ValueText == "Pattern");

		if (patternProperty is null)
		{
			return;
		}

		if (!HasStringSyntaxRouteAttribute(patternProperty))
		{
			context.ReportDiagnostic(Diagnostic.Create(
				PatternPropertyMissingStringSyntaxAttribute,
				patternProperty.Identifier.GetLocation()));
		}

		var routePattern = GetPropertyStringLiteralValue(patternProperty);
		if (routePattern is null)
		{
			return;
		}

		var handleAsyncMethod = classDeclaration.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "HandleAsync");

		if (handleAsyncMethod is null)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				HandleAsyncMethodMissing,
				classDeclaration.Identifier.GetLocation(),
				classSymbol.Name));

			return;
		}

		var handleAsyncSymbol = context.SemanticModel.GetDeclaredSymbol(handleAsyncMethod, context.CancellationToken);
		if (handleAsyncSymbol is null)
		{
			return;
		}

		var handlerParameters = GetMethodParameters(context, handleAsyncSymbol);
		AnalyzeRouteParameters(context, patternProperty, routePattern, handlerParameters);
	}

	private static string? GetPropertyStringLiteralValue(PropertyDeclarationSyntax property)
	{
		if (property.ExpressionBody?.Expression is LiteralExpressionSyntax literal &&
			literal.IsKind(SyntaxKind.StringLiteralExpression))
		{
			return literal.Token.ValueText;
		}

		return null;
	}

	private static bool HasStringSyntaxRouteAttribute(PropertyDeclarationSyntax property)
	{
		foreach (var attributeList in property.AttributeLists)
		{
			foreach (var attribute in attributeList.Attributes)
			{
				var name = attribute.Name.ToString();
				if (name != "StringSyntax" && name != "StringSyntaxAttribute")
				{
					continue;
				}

				var args = attribute.ArgumentList?.Arguments;
				if (args is null || args.Value.Count == 0)
				{
					continue;
				}

				var firstArg = args.Value[0].Expression;
				if (firstArg is LiteralExpressionSyntax lit &&
					lit.IsKind(SyntaxKind.StringLiteralExpression) &&
					string.Equals(lit.Token.ValueText, "Route", StringComparison.Ordinal))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static LambdaParameter[] GetMethodParameters(
		SyntaxNodeAnalysisContext context,
		IMethodSymbol method)
	{
		return method.Parameters
			.Select(p => (
				Symbol: p,
				Syntax: p.DeclaringSyntaxReferences
					.FirstOrDefault()
					?.GetSyntax(context.CancellationToken) as ParameterSyntax))
			.Where(p => p.Syntax is not null)
			.Select(p => new LambdaParameter(p.Syntax!, p.Symbol))
			.ToArray();
	}

	private static void AnalyzeRouteParameters(
		SyntaxNodeAnalysisContext context,
		PropertyDeclarationSyntax patternProperty,
		string routePattern,
		LambdaParameter[] handlerParameters)
	{
		var routeParameters = ParseRouteParameters(routePattern);

		var filteredParameters = handlerParameters
			.Where(parameter => !ShouldIgnoreParameter(parameter.Symbol))
			.ToArray();

		var parametersByName = filteredParameters
			.ToDictionary(
				parameter => parameter.Symbol.Name,
				parameter => parameter,
				StringComparer.OrdinalIgnoreCase);

		foreach (var routeParameter in routeParameters)
		{
			if (!parametersByName.TryGetValue(routeParameter.Name, out var matchingParameter))
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						RouteParameterMissingFromHandler,
						patternProperty.GetLocation(),
						routeParameter.Name));

				continue;
			}

			if (routeParameter.Constraint is not null)
			{
				AnalyzeConstraint(
					context,
					patternProperty.GetLocation(),
					routeParameter,
					matchingParameter.Symbol);
			}
		}

		var routeParameterNames = new HashSet<string>(
			routeParameters.Select(parameter => parameter.Name),
			StringComparer.OrdinalIgnoreCase);

		foreach (var parameter in filteredParameters)
		{
			if (!routeParameterNames.Contains(parameter.Symbol.Name))
			{
				context.ReportDiagnostic(
					Diagnostic.Create(
						HandlerParameterMissingFromRoute,
						parameter.Syntax.GetLocation(),
						parameter.Symbol.Name));
			}
		}
	}

	private static void AnalyzeConstraint(
		SyntaxNodeAnalysisContext context,
		Location location,
		RouteParameter routeParameter,
		IParameterSymbol lambdaParameter)
	{
		var expectedTypeName = GetExpectedTypeNameForConstraint(routeParameter.Constraint!);
		if (expectedTypeName is null)
		{
			return;
		}

		var actualType = UnwrapNullable(lambdaParameter.Type);

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

	private sealed record LambdaParameter(ParameterSyntax Syntax, IParameterSymbol Symbol);
}
