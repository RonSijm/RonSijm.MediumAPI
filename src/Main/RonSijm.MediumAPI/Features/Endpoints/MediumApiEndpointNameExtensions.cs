using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>Controls the fallback name supplied to otherwise unnamed MediumAPI endpoint mappings.</summary>
public static class MediumApiEndpointNameExtensions
{
    /// <summary>Suppresses only the generated fallback name for this mapping; explicit names remain unchanged.</summary>
    public static RouteHandlerBuilder WithoutDefaultEndpointName(this RouteHandlerBuilder endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return endpoint.WithMetadata(new SuppressDefaultEndpointNameMetadata());
    }

    /// <summary>Suppresses fallback names in this group and its descendants without removing explicit names.</summary>
    public static RouteGroupBuilder WithoutDefaultEndpointName(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return group.WithMetadata(new SuppressDefaultEndpointNameMetadata());
    }

    internal static void ApplyDefaultEndpointName<TEndpoint>(RouteHandlerBuilder endpoint) where TEndpoint : IEndpoint
    {
        const string suffix = "Endpoint";
        var className = typeof(TEndpoint).Name;
        var defaultName = className.Length > suffix.Length && className.EndsWith(suffix, StringComparison.Ordinal)
            ? className[..^suffix.Length]
            : className;
        endpoint.Finally(builder =>
        {
            if (builder.Metadata.Any(metadata => metadata is SuppressDefaultEndpointNameMetadata)
                || builder.Metadata.OfType<IEndpointNameMetadata>().LastOrDefault()?.EndpointName is not null)
            {
                return;
            }

            builder.Metadata.Add(new EndpointNameMetadata(defaultName));
            if (builder.Metadata.OfType<IRouteNameMetadata>().LastOrDefault()?.RouteName is null)
            {
                builder.Metadata.Add(new RouteNameMetadata(defaultName));
            }
        });
    }

    private sealed class SuppressDefaultEndpointNameMetadata { }
}