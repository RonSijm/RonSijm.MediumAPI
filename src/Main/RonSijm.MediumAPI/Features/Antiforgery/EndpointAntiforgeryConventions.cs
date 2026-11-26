using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace RonSijm.MediumAPI;

internal static class EndpointAntiforgeryConventions
{
    internal static void ApplyDeclaredDisable(Type type, IEndpointConventionBuilder builder) => type.GetCustomAttribute<DisableEndpointAntiforgeryAttribute>(inherit: false)?.Apply(builder);
}