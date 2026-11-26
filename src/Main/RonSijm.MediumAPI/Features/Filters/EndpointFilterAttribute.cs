using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>Base marker used to apply a supported endpoint filter declaration to a handler or namespace group.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class EndpointFilterAttribute : Attribute
{
    internal abstract void Apply(RouteHandlerBuilder endpoint);
    internal abstract void Apply(RouteGroupBuilder group);
}

/// <summary>Registers one native filter with any MediumAPI startup conventions; repeated declarations register repeated filters.</summary>
public class EndpointFilterAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilter> : EndpointFilterAttribute where TFilter : class, IEndpointFilter
{
    internal override void Apply(RouteHandlerBuilder endpoint) => endpoint.AddMediumApiFilter<TFilter>();
    internal override void Apply(RouteGroupBuilder group) => group.AddMediumApiFilter<TFilter>();
}