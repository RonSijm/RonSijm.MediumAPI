using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RonSijm.MediumAPI.Analyser;

/// <summary>Reads proven route arguments of immutable constructor-bound native AsParameters classes and records.</summary>
internal static class EndpointConstructorRouteBinding
{
    internal static EndpointAggregateRouteMember[] Read(Compilation compilation, IParameterSymbol parameter, CancellationToken cancellationToken)
    {
        var aggregateAttribute = GetNativeType(compilation, "Microsoft.AspNetCore.Http.AsParametersAttribute");
        if (aggregateAttribute is null || parameter.RefKind != RefKind.None || parameter.NullableAnnotation != NullableAnnotation.NotAnnotated
            || !parameter.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, aggregateAttribute))
            || EndpointParameterBinding.Read(compilation, parameter).Kind != EndpointBindingSourceKind.Aggregate
            || parameter.Type is not INamedTypeSymbol type || type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsStatic
            || type.IsGenericType || type.BaseType?.SpecialType != SpecialType.System_Object || type.DeclaringSyntaxReferences.Length == 0
            || !type.DeclaringSyntaxReferences.All(reference => reference.GetSyntax(cancellationToken) is ClassDeclarationSyntax or RecordDeclarationSyntax))
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

        // The parameterless native path is deliberately left to the unchanged mutable reader.
        var constructors = type.InstanceConstructors.Where(constructor => constructor.DeclaredAccessibility == Accessibility.Public).ToArray();
        if (constructors.Length != 1 || constructors[0].Parameters.Length == 0 || constructors[0].IsVararg)
        {
            return [];
        }

        var properties = type.GetMembers().OfType<IPropertySymbol>().Where(property => property.DeclaredAccessibility == Accessibility.Public).ToArray();
        for (var index = 0; index < properties.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var property = properties[index];
            if (property.IsStatic || property.IsIndexer || property.IsVirtual || property.IsOverride || property.IsAbstract
                || property.ReturnsByRef || property.ReturnsByRefReadonly)
            {
                return [];
            }
            // Native lookup keys use case-insensitive names and exact CLR property types.
            if (properties.Take(index).Any(previous => string.Equals(previous.Name, property.Name, StringComparison.OrdinalIgnoreCase)
                                                       && SymbolEqualityComparer.Default.Equals(previous.Type, property.Type)))
            {
                return [];
            }
        }

        var sourceContracts = SourceMetadataNames.Select(name => GetNativeType(compilation, name)).Where(symbol => symbol is not null).ToArray();
        var sourceAttributes = SourceAttributeNames.Select(name => GetNativeType(compilation, name)).Where(symbol => symbol is not null).ToArray();
        var keyedServices = GetNativeType(compilation, "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute");
        var members = new List<EndpointAggregateRouteMember>();
        var matchedProperties = new HashSet<IPropertySymbol>(SymbolEqualityComparer.Default);
        var routeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var constructorParameter in constructors[0].Parameters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (constructorParameter.RefKind != RefKind.None || constructorParameter.Type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer || constructorParameter.Type is INamedTypeSymbol { IsRefLikeType: true })
            {
                return [];
            }

            var matches = properties.Where(property => string.Equals(property.Name, constructorParameter.Name, StringComparison.OrdinalIgnoreCase)
                                                       && SymbolEqualityComparer.Default.Equals(property.Type, constructorParameter.Type)).ToArray();
            if (matches.Length != 1)
            {
                return [];
            }

            var property = matches[0];
            if (!matchedProperties.Add(property) || property.GetMethod?.DeclaredAccessibility != Accessibility.Public
                                                 || property.SetMethod is { IsInitOnly: false })
            {
                return [];
            }

            var constructorSources = ReadSources(constructorParameter.GetAttributes());
            var propertySources = ReadSources(property.GetAttributes());
            var sources = constructorSources.AddRange(propertySources);
            if (sources.Length > 1 || sources.Any(attribute => !sourceAttributes.Any(source => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, source))))
            {
                return [];
            }

            var binding = EndpointParameterBinding.Read(compilation, constructorParameter, property);
            if (binding.IsOpaque || binding.Kind == EndpointBindingSourceKind.Aggregate)
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

            var location = constructorSources.Length > 0 ? ParameterLocation(constructorParameter, cancellationToken) : PropertyLocation(property, constructorParameter, cancellationToken);
            if (location is null)
            {
                return [];
            }

            members.Add(new EndpointAggregateRouteMember(property, routeName, location));
        }

        return members.ToArray();

        ImmutableArray<AttributeData> ReadSources(ImmutableArray<AttributeData> attributes) => attributes.Where(attribute => attribute.AttributeClass is { } attributeType
                                                                                                                             && (attributeType.AllInterfaces.Any(contract => sourceContracts.Any(source => SymbolEqualityComparer.Default.Equals(contract, source)))
                                                                                                                                 || DerivesFrom(attributeType, keyedServices) || SymbolEqualityComparer.Default.Equals(attributeType, aggregateAttribute))).ToImmutableArray();
    }

    private static Location? ParameterLocation(IParameterSymbol parameter, CancellationToken cancellationToken) => parameter.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(cancellationToken)).OfType<ParameterSyntax>().FirstOrDefault()?.Identifier.GetLocation();

    private static Location? PropertyLocation(IPropertySymbol property, IParameterSymbol parameter, CancellationToken cancellationToken)
    {
        foreach (var reference in property.DeclaringSyntaxReferences)
        {
            var syntax = reference.GetSyntax(cancellationToken);
            if (syntax is PropertyDeclarationSyntax declaration)
            {
                return declaration.Identifier.GetLocation();
            }

            if (syntax is ParameterSyntax positional)
            {
                return positional.Identifier.GetLocation();
            }
        }

        // Positional record properties can be synthesized from the primary constructor parameter.
        return property.IsImplicitlyDeclared ? ParameterLocation(parameter, cancellationToken) : null;
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