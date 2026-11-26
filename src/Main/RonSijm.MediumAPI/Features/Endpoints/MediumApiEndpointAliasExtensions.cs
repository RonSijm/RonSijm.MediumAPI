using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>Maps explicit aliases through the existing endpoint mapper and exposes their native aggregate convention builder.</summary>
public static class MediumApiEndpointAliasExtensions
{
    /// <summary>Maps each supplied pattern in input order, suppressing only the fallback endpoint name on each alias.</summary>
    /// <remarks>
    /// The patterns are snapshotted once and checked for null entries before mapping starts. Empty input maps nothing and does not look up an endpoint registration.
    /// Other validation and mapping failures retain the existing behavior; a later failure does not roll back earlier aliases. Duplicates and explicit names are preserved.
    /// The current route builder supplies group policies; automatic namespace ownership is not applied. Existing endpoint Configure runs for each alias.
    /// Conventions added to the returned native aggregate fan out to its children and may share metadata objects. A combined filter registration applies its optional Configure once to the aggregate.
    /// </remarks>
    public static RouteHandlerBuilder MapMediumApiEndpointAliases<TEndpoint>(this IEndpointRouteBuilder builder, IEnumerable<string> patterns) where TEndpoint : IEndpoint
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(patterns);
        var aliases = patterns.ToArray();
        if (aliases.Any(static pattern => pattern is null))
        {
            throw new ArgumentException("Alias patterns cannot contain null entries.", nameof(patterns));
        }

        var children = new IEndpointConventionBuilder[aliases.Length];
        for (var index = 0; index < aliases.Length; index++)
        {
            children[index] = builder.MapMediumApiEndpoint<TEndpoint>(aliases[index]).WithoutDefaultEndpointName();
        }

        return new RouteHandlerBuilder(children);
    }
}