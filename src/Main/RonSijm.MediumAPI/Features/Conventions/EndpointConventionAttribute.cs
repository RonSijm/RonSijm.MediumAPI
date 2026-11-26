using Microsoft.AspNetCore.Builder;

namespace RonSijm.MediumAPI;

/// <summary>Base marker for an explicitly requested native startup convention on an endpoint, handler or namespace group.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class EndpointConventionAttribute : Attribute
{
    internal abstract void Apply(IEndpointConventionBuilder endpoint);
}

/// <summary>Applies one static convention per registration without creating an instance or importing its attributes.</summary>
public class EndpointConventionAttribute<TConvention> : EndpointConventionAttribute where TConvention : IMediumApiEndpointConvention
{
    internal override void Apply(IEndpointConventionBuilder endpoint)
    {
        TConvention.Configure(endpoint);
    }
}