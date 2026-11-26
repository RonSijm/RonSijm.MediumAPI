using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace RonSijm.MediumAPI;

/// <summary>Applies directly declared response fallbacks without importing CLR base-class policies or caching host state.</summary>
internal static class EndpointResponseFallbackDeclarations
{
    internal static void ApplyDeclaredFallbacks(Type type, IEndpointConventionBuilder builder)
    {
        foreach (var fallback in type.GetCustomAttributes<EndpointResponseFallbackAttribute>(inherit: false))
        {
            fallback.Apply(builder);
        }
    }
}