using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RonSijm.MediumAPI.Analyser;

/// <summary>Reads proven explicit route properties of a narrow native AsParameters mutable struct.</summary>
internal static class EndpointStructRouteBinding
{
    internal static EndpointAggregateRouteMember[] Read(Compilation compilation, IParameterSymbol parameter, CancellationToken cancellationToken)
    {
        var aggregateAttribute = GetNativeType(compilation, "Microsoft.AspNetCore.Http.AsParametersAttribute");
        if (aggregateAttribute is null || parameter.RefKind != RefKind.None
                                       || !parameter.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, aggregateAttribute))
                                       || EndpointParameterBinding.Read(compilation, parameter).Kind != EndpointBindingSourceKind.Aggregate
                                       || parameter.Type is not INamedTypeSymbol type || type.TypeKind != TypeKind.Struct || type.IsReadOnly || type.IsRefLikeType || type.IsRecord
                                       || type.IsGenericType || type.DeclaringSyntaxReferences.Length == 0
                                       || !type.DeclaringSyntaxReferences.All(reference => reference.GetSyntax(cancellationToken) is StructDeclarationSyntax { ParameterList: null }))
        {
            return [];
        }

        for (var container = type.ContainingType; container is not null; container = container.ContainingType)
        {
            if (container.IsGenericType)
            {
                return [];
            }
        }

        // Reflection does not expose an implicit value-type default constructor. A sole public
        // parameterized constructor selects native constructor binding, even if Roslyn lists a default.
        var constructors = type.InstanceConstructors.Where(constructor => constructor.DeclaredAccessibility == Accessibility.Public && !constructor.IsImplicitlyDeclared).ToArray();
        if (constructors.Length > 0 && !constructors.Any(constructor => constructor.Parameters.Length == 0 && !constructor.IsVararg))
        {
            return [];
        }

        var sourceContracts = SourceMetadataNames.Select(name => GetNativeType(compilation, name)).Where(symbol => symbol is not null).ToArray();
        var sourceAttributes = SourceAttributeNames.Select(name => GetNativeType(compilation, name)).Where(symbol => symbol is not null).ToArray();
        var keyedServices = GetNativeType(compilation, "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute");
        var members = new List<EndpointAggregateRouteMember>();
        var routeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in type.GetMembers().OfType<IPropertySymbol>().Where(property => property.DeclaredAccessibility == Accessibility.Public))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (property.IsStatic || property.IsIndexer || property.IsVirtual || property.IsOverride || property.IsAbstract
                || property.ReturnsByRef || property.ReturnsByRefReadonly || property.GetMethod?.DeclaredAccessibility != Accessibility.Public
                || property.SetMethod?.DeclaredAccessibility != Accessibility.Public || property.SetMethod?.IsInitOnly == true
                || property.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer || property.Type is INamedTypeSymbol { IsRefLikeType: true }
                || property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) is not PropertyDeclarationSyntax syntax)
            {
                return [];
            }

            var binding = EndpointParameterBinding.Read(compilation, property);
            if (binding.IsOpaque || binding.Kind == EndpointBindingSourceKind.Aggregate)
            {
                return [];
            }

            var sources = property.GetAttributes().Where(attribute => attribute.AttributeClass is { } attributeType
                                                                      && (attributeType.AllInterfaces.Any(contract => sourceContracts.Any(source => SymbolEqualityComparer.Default.Equals(contract, source)))
                                                                          || DerivesFrom(attributeType, keyedServices) || SymbolEqualityComparer.Default.Equals(attributeType, aggregateAttribute))).ToArray();
            if (sources.Length > 1 || sources.Any(attribute => !sourceAttributes.Any(source => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, source))))
            {
                return [];
            }

            if (binding.Kind != EndpointBindingSourceKind.Route || binding.RouteName is not { } routeName)
            {
                continue;
            }

            if (!routeNames.Add(routeName))
            {
                return [];
            }

            members.Add(new EndpointAggregateRouteMember(property, routeName, syntax.Identifier.GetLocation()));
        }

        return members.ToArray();
    }

    private static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol? contract)
    {
        if (contract is null)
        {
            return false;
        }

        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, contract))
            {
                return true;
            }
        }

        return false;
    }

    private static INamedTypeSymbol? GetNativeType(Compilation compilation, string metadataName)
    {
        var type = compilation.GetTypeByMetadataName(metadataName);
        return type is not null && type.DeclaringSyntaxReferences.Length == 0 && !SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly) ? type : null;
    }

    private static readonly string[] SourceAttributeNames =
    [
        "Microsoft.AspNetCore.Mvc.FromRouteAttribute",
        "Microsoft.AspNetCore.Mvc.FromQueryAttribute",
        "Microsoft.AspNetCore.Mvc.FromHeaderAttribute",
        "Microsoft.AspNetCore.Mvc.FromBodyAttribute",
        "Microsoft.AspNetCore.Mvc.FromFormAttribute",
        "Microsoft.AspNetCore.Mvc.FromServicesAttribute",
        "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute"
    ];

    private static readonly string[] SourceMetadataNames =
    [
        "Microsoft.AspNetCore.Http.Metadata.IFromRouteMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromQueryMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromHeaderMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromBodyMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromFormMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromServiceMetadata"
    ];
}