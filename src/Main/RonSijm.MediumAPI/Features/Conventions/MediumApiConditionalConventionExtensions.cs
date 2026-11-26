using Microsoft.AspNetCore.Builder;

namespace RonSijm.MediumAPI;

/// <summary>Conditionally registers ordinary native endpoint conventions without adding a request filter or changing materialization order.</summary>
public static class MediumApiConditionalConventionExtensions
{
    /// <summary>Registers one ordinary Add callback and returns the same builder. The callback evaluates the condition against its current native EndpointBuilder and invokes the convention only when true.</summary>
    /// <remarks>Neither delegate runs during registration. Native materialization may run the callback again; no result is cached and no Finally callback is added. Exceptions propagate unchanged, including any partial mutations already made by either delegate. This is startup convention composition, not request-time middleware.</remarks>
    public static TBuilder AddConditionalConvention<TBuilder>(this TBuilder builder, Func<EndpointBuilder, bool> condition, Action<EndpointBuilder> convention) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(convention);
        builder.Add(endpoint =>
        {
            if (condition(endpoint))
            {
                convention(endpoint);
            }
        });
        return builder;
    }
}