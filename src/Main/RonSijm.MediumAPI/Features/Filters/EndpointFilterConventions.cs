using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace RonSijm.MediumAPI;

/// <summary>Dispatches optional startup conventions through the existing static interface contract.</summary>
internal static class EndpointFilterConventions
{
    internal static void Apply<TFilter>(IEndpointConventionBuilder builder) where TFilter : class, IEndpointFilter => Configuration<TFilter>.Configure?.Invoke(builder);

    private static void ConfigureMediumApiFilter<TFilter>(IEndpointConventionBuilder builder) where TFilter : class, IMediumApiEndpointFilter => TFilter.Configure(builder);

    private static class Configuration<TFilter> where TFilter : class, IEndpointFilter
    {
        // Cache only the dispatch delegate. Configure still runs for every registration,
        // outside type initialization, so its exceptions retain their original identity.
        internal static readonly Action<IEndpointConventionBuilder>? Configure = CreateConfigure();

        private static Action<IEndpointConventionBuilder>? CreateConfigure()
        {
            if (!typeof(IMediumApiEndpointFilter).IsAssignableFrom(typeof(TFilter)))
            {
                return null;
            }
            // Constrained interface dispatch supports inherited and explicit implementations
            // without discovering similarly named methods on ordinary native filters.
            return typeof(EndpointFilterConventions).GetMethod(nameof(ConfigureMediumApiFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(typeof(TFilter)).CreateDelegate<Action<IEndpointConventionBuilder>>();
        }
    }
}