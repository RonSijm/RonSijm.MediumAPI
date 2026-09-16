using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

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
/// Generates <c>AddMediumApiEndpoints</c> and <c>MapMediumApiEndpoints</c> extension methods
/// by discovering all <c>IEndpointAdapter</c> implementations at compile time.
/// </summary>
[Generator]
public sealed class EndpointRegistrationGenerator : IIncrementalGenerator
{
	internal static readonly DiagnosticDescriptor EndpointMetadataUnparseableDescriptor = new(
		id: EndpointDiagnosticIds.EndpointMetadataUnparseable,
		title: "Endpoint metadata cannot be read by the source generator",
		messageFormat: "Endpoint '{0}' will be skipped: {1}. Use an expression-bodied property with a string literal for 'Pattern', a direct 'HttpVerb.X' for 'Verb', and a string literal for 'AuthorizationPolicy'.",
		category: "EndpointRouting",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

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
				predicate: static (node, _) => IsCandidateClass(node),
				transform: static (ctx, _) => GetEndpointInfo(ctx))
			.Where(static result => result is not null)
			.Select(static (result, _) => result!);

		var combined = endpoints.Collect().Combine(rootNamespace);

		context.RegisterSourceOutput(
			combined,
			static (ctx, pair) => Emit(ctx, pair.Left, pair.Right));
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
		LocationInfo Location);

	private sealed record EndpointParseResult(EndpointInfo? Info, DiagnosticInfo? Diagnostic);

	private static EndpointParseResult? GetEndpointInfo(GeneratorSyntaxContext context)
	{
		var classDecl = (ClassDeclarationSyntax)context.Node;

		if (context.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol symbol)
		{
			return null;
		}

		if (symbol.IsAbstract || symbol.IsStatic)
		{
			return null;
		}

		if (!symbol.AllInterfaces.Any(i => i.Name == "IEndpointAdapter"))
		{
			return null;
		}

		var classLocation = LocationInfo.From(classDecl.Identifier);

		var patternValue = GetStringLiteralPropertyValue(classDecl, "Pattern");
		if (patternValue is null)
		{
			return UnparseableResult(symbol, classLocation, "the 'Pattern' property is missing or is not an expression-bodied string literal");
		}

		var verbMemberName = GetMemberAccessPropertyName(classDecl, "Verb");
		if (verbMemberName is null)
		{
			return UnparseableResult(symbol, classLocation, "the 'Verb' property is missing or is not an expression-bodied 'HttpVerb.X' access");
		}

		var authPolicy = TryGetAuthPolicy(context, classDecl, out var authPolicyUnparseable);

		if (authPolicyUnparseable)
		{
			return UnparseableResult(symbol, classLocation, "the 'AuthorizationPolicy' property uses an expression the generator cannot evaluate (only null, a string literal, or an interpolation of const-string fields is supported)");
		}

		var info = new EndpointInfo(
			FullyQualifiedTypeName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			PatternValue: patternValue,
			VerbName: verbMemberName,
			AuthPolicy: authPolicy,
			Location: classLocation);

		return new EndpointParseResult(info, Diagnostic: null);
	}

	private static EndpointParseResult UnparseableResult(INamedTypeSymbol symbol, LocationInfo location, string reason)
	{
		var diagnostic = new DiagnosticInfo(
			EndpointDiagnosticIds.EndpointMetadataUnparseable,
			location,
			new EquatableArray<string>(ImmutableArray.Create(symbol.Name, reason)));
		return new EndpointParseResult(Info: null, Diagnostic: diagnostic);
	}

	private static string? GetStringLiteralPropertyValue(ClassDeclarationSyntax classDecl, string propertyName)
	{
		var property = classDecl.Members
			.OfType<PropertyDeclarationSyntax>()
			.FirstOrDefault(p => p.Identifier.ValueText == propertyName);

		if (property?.ExpressionBody?.Expression is LiteralExpressionSyntax literal &&
			literal.IsKind(SyntaxKind.StringLiteralExpression))
		{
			return literal.Token.ValueText;
		}

		return null;
	}

	private static string? GetMemberAccessPropertyName(ClassDeclarationSyntax classDecl, string propertyName)
	{
		var property = classDecl.Members
			.OfType<PropertyDeclarationSyntax>()
			.FirstOrDefault(p => p.Identifier.ValueText == propertyName);

		if (property?.ExpressionBody?.Expression is MemberAccessExpressionSyntax memberAccess)
		{
			return memberAccess.Name.Identifier.ValueText;
		}

		return null;
	}

	private static string? TryGetAuthPolicy(GeneratorSyntaxContext context, ClassDeclarationSyntax classDecl, out bool unparseable)
	{
		unparseable = false;

		var property = classDecl.Members
			.OfType<PropertyDeclarationSyntax>()
			.FirstOrDefault(p => p.Identifier.ValueText == "AuthorizationPolicy");

		if (property?.ExpressionBody?.Expression is null)
		{
			return null;
		}

		var expr = property.ExpressionBody.Expression;

		if (expr.IsKind(SyntaxKind.NullLiteralExpression))
		{
			return null;
		}

		if (expr is LiteralExpressionSyntax lit && lit.IsKind(SyntaxKind.StringLiteralExpression))
		{
			return lit.Token.ValueText;
		}

		if (expr is InterpolatedStringExpressionSyntax interpolated)
		{
			var value = TryEvaluateInterpolatedString(context, interpolated);
			if (value is null)
			{
				unparseable = true;
			}
			return value;
		}

		unparseable = true;
		return null;
	}

	private static string? TryEvaluateInterpolatedString(
		GeneratorSyntaxContext context,
		InterpolatedStringExpressionSyntax interpolated)
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
				var symbolInfo = context.SemanticModel.GetSymbolInfo(interpolation.Expression);
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

	private static void Emit(SourceProductionContext context, ImmutableArray<EndpointParseResult> results, string rootNamespace)
	{
		foreach (var result in results)
		{
			if (result.Diagnostic is { } diag)
			{
				var descriptor = diag.Id switch
				{
					EndpointDiagnosticIds.EndpointMetadataUnparseable => EndpointMetadataUnparseableDescriptor,
					_ => null,
				};

				if (descriptor is not null)
				{
					context.ReportDiagnostic(Diagnostic.Create(
						descriptor,
						diag.Location?.ToLocation() ?? Location.None,
						diag.MessageArgs.Values.Cast<object?>().ToArray()));
				}
			}
		}

		var infos = results
			.Where(r => r.Info is not null)
			.Select(r => r.Info!)
			.ToImmutableArray();

		if (infos.IsEmpty)
		{
			return;
		}

		var ordered = infos.OrderBy(i => i.FullyQualifiedTypeName, StringComparer.Ordinal).ToArray();

		GenerateAddMediumApiEndpoints(context, ordered, rootNamespace);
		GenerateMapMediumApiEndpoints(context, ordered, rootNamespace);
	}

	private static void GenerateAddMediumApiEndpoints(SourceProductionContext context, EndpointInfo[] infos, string rootNamespace)
	{
		var sb = new StringBuilder();

		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("#nullable enable");
		sb.AppendLine();
		sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
		sb.AppendLine();
		sb.AppendLine($"namespace {rootNamespace};");
		sb.AppendLine();
		sb.AppendLine("public static partial class MediumApiEndpointExtensions");
		sb.AppendLine("{");
		sb.AppendLine("	public static IServiceCollection AddMediumApiEndpoints(this IServiceCollection services)");
		sb.AppendLine("		=> services;");
		sb.AppendLine("}");

		context.AddSource("MediumApiEndpointExtensions.Add.g.cs", sb.ToString());
	}

	private static void GenerateMapMediumApiEndpoints(SourceProductionContext context, EndpointInfo[] infos, string rootNamespace)
	{
		var sb = new StringBuilder();

		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("#nullable enable");
		sb.AppendLine();
		sb.AppendLine("using Microsoft.AspNetCore.Builder;");
		sb.AppendLine("using Microsoft.AspNetCore.Http;");
		sb.AppendLine("using Microsoft.AspNetCore.Routing;");
		sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
		sb.AppendLine();
		sb.AppendLine($"namespace {rootNamespace};");
		sb.AppendLine();
		sb.AppendLine("public static partial class MediumApiEndpointExtensions");
		sb.AppendLine("{");
		sb.AppendLine("	public static IEndpointRouteBuilder MapMediumApiEndpoints(this IEndpointRouteBuilder builder)");
		sb.AppendLine("	{");

		foreach (var info in infos)
		{
			var httpVerb = info.VerbName.ToUpperInvariant();

			sb.AppendLine($"		// {info.FullyQualifiedTypeName}");
			sb.AppendLine("		{");
			sb.AppendLine($"			var method = typeof({info.FullyQualifiedTypeName}).GetMethod(\"HandleAsync\")!;");
			sb.AppendLine("			var result = RequestDelegateFactory.Create(");
			sb.AppendLine("				method,");
			sb.AppendLine($"				ctx => ActivatorUtilities.CreateInstance<{info.FullyQualifiedTypeName}>(ctx.RequestServices),");
			sb.AppendLine("				new RequestDelegateFactoryOptions { ServiceProvider = builder.ServiceProvider });");
			sb.AppendLine($"			var mapping = builder.MapMethods(\"{info.PatternValue}\", [\"{httpVerb}\"], result.RequestDelegate);");
			sb.AppendLine("			foreach (var md in result.EndpointMetadata)");
			sb.AppendLine("				mapping.WithMetadata(md);");

			if (info.AuthPolicy is not null)
			{
				sb.AppendLine($"			mapping.RequireAuthorization(\"{info.AuthPolicy}\");");
			}

			sb.AppendLine("		}");
		}

		sb.AppendLine("		return builder;");
		sb.AppendLine("	}");
		sb.AppendLine("}");

		context.AddSource("MediumApiEndpointExtensions.Map.g.cs", sb.ToString());
	}
}
