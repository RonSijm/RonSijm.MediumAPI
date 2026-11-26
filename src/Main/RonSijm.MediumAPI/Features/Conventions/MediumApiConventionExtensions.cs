using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>Applies static startup conventions to native endpoint and group builders without adding a request filter.</summary>
public static class MediumApiConventionExtensions
{
    /// <summary>Runs Configure through an interface-typed native builder, allowing explicit convention composition, and returns that same builder.</summary>
    public static IEndpointConventionBuilder AddMediumApiConvention<TConvention>(this IEndpointConventionBuilder endpoint) where TConvention : IMediumApiEndpointConvention
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        TConvention.Configure(endpoint);
        return endpoint;
    }

    /// <summary>Runs Configure for this endpoint registration and returns the same builder. Later native conventions retain their order.</summary>
    public static RouteHandlerBuilder AddMediumApiConvention<TConvention>(this RouteHandlerBuilder endpoint) where TConvention : IMediumApiEndpointConvention
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        TConvention.Configure(endpoint);
        return endpoint;
    }

    /// <summary>Runs Configure for this group registration and returns the same builder. Later native conventions retain their order.</summary>
    public static RouteGroupBuilder AddMediumApiConvention<TConvention>(this RouteGroupBuilder group) where TConvention : IMediumApiEndpointConvention
    {
        ArgumentNullException.ThrowIfNull(group);
        TConvention.Configure(group);
        return group;
    }
}