using Microsoft.AspNetCore.Builder;

namespace RonSijm.MediumAPI;

/// <summary>Applies ASP.NET Core's native final antiforgery opt-out convention to an endpoint, namespace definition or registered MediumAPI filter.</summary>
/// <remarks>This disables validation through native metadata; it does not register middleware or change the order of other native final conventions.</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DisableEndpointAntiforgeryAttribute : Attribute
{
    internal void Apply(IEndpointConventionBuilder builder) => builder.DisableAntiforgery();
}