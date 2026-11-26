using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RonSijm.MediumAPI.Analyser;

internal enum EndpointBindingSourceKind
{
    None,
    Route,
    NonRoute,
    Aggregate,
    Opaque
}

/// <summary>Binding-source evidence from symbols only. A null route name is unknown, not the parameter's default name.</summary>
internal readonly struct EndpointParameterBinding
{
    private EndpointParameterBinding(EndpointBindingSourceKind kind, string? routeName)
    {
        Kind = kind;
        RouteName = routeName;
    }

    internal EndpointBindingSourceKind Kind { get; }
    internal string? RouteName { get; }
    internal bool HasExplicitSource => Kind != EndpointBindingSourceKind.None;
    internal bool IsOpaque => (Kind is EndpointBindingSourceKind.Aggregate or EndpointBindingSourceKind.Opaque) || Kind == EndpointBindingSourceKind.Route && RouteName is null;

    internal static EndpointParameterBinding Read(Compilation compilation, IParameterSymbol parameter) => Read(compilation, parameter.GetAttributes(), parameter.Name);

    internal static EndpointParameterBinding Read(Compilation compilation, IPropertySymbol property) => Read(compilation, property.GetAttributes(), property.Name);

    internal static EndpointParameterBinding Read(Compilation compilation, IParameterSymbol parameter, IPropertySymbol property) => Read(compilation, parameter.GetAttributes().AddRange(property.GetAttributes()), property.Name);

    private static EndpointParameterBinding Read(Compilation compilation, ImmutableArray<AttributeData> attributes, string defaultName)
    {
        if (attributes.IsDefaultOrEmpty)
        {
            return new(EndpointBindingSourceKind.None, defaultName);
        }

        var nativeRoute = GetNativeType(compilation, "Microsoft.AspNetCore.Http.Metadata.IFromRouteMetadata");
        var nativeRouteAttribute = GetNativeType(compilation, "Microsoft.AspNetCore.Mvc.FromRouteAttribute");
        var nativeAggregate = GetNativeType(compilation, "Microsoft.AspNetCore.Http.AsParametersAttribute");
        var nativeKeyedServices = GetNativeType(compilation, "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute");
        var nonRouteContracts = NonRouteMetadataNames.Select(name => GetNativeType(compilation, name)).Where(type => type is not null).ToArray();
        var routes = 0;
        var nonRoute = false;
        var aggregate = false;
        string? routeName = null;
        foreach (var attribute in attributes)
        {
            var type = attribute.AttributeClass;
            if (type is null)
            {
                continue;
            }

            aggregate |= DerivesFrom(type, nativeAggregate);
            nonRoute |= DerivesFrom(type, nativeKeyedServices);
            var interfaces = type.AllInterfaces;
            if (nativeRoute is not null && interfaces.Any(iface => SymbolEqualityComparer.Default.Equals(iface, nativeRoute)))
            {
                routes++;
                // Only the exact framework attribute has a known setter/getter contract.
                // Custom route metadata can transform constructor or named arguments.
                routeName = routes == 1 && SymbolEqualityComparer.Default.Equals(type, nativeRouteAttribute) && TryReadNativeRouteName(attribute, defaultName, out var name) ? name : null;
            }

            nonRoute |= interfaces.Any(iface => nonRouteContracts.Any(contract => SymbolEqualityComparer.Default.Equals(iface, contract)));
        }

        // Do not resolve conflicting metadata using a guessed native evaluation order.
        if ((aggregate && (routes > 0 || nonRoute)) || (routes > 0 && nonRoute))
        {
            return new(EndpointBindingSourceKind.Opaque, null);
        }

        if (aggregate)
        {
            return new(EndpointBindingSourceKind.Aggregate, null);
        }

        if (routes > 0)
        {
            return new(EndpointBindingSourceKind.Route, routeName);
        }

        return nonRoute ? new(EndpointBindingSourceKind.NonRoute, null) : new(EndpointBindingSourceKind.None, defaultName);
    }

    private static bool TryReadNativeRouteName(AttributeData attribute, string parameterName, out string? name)
    {
        foreach (var argument in attribute.NamedArguments.Where(argument => argument.Key == "Name"))
        {
            if (argument.Value.Kind != TypedConstantKind.Primitive || argument.Value.Value is not (null or string))
            {
                name = null;
                return false;
            }

            // Empty is intentionally retained. Native route binding uses ??, not an empty-string fallback.
            name = argument.Value.Value as string ?? parameterName;
            return true;
        }

        name = parameterName;
        return true;
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

    private static readonly string[] NonRouteMetadataNames =
    [
        "Microsoft.AspNetCore.Http.Metadata.IFromQueryMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromHeaderMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromBodyMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromFormMetadata",
        "Microsoft.AspNetCore.Http.Metadata.IFromServiceMetadata"
    ];
}