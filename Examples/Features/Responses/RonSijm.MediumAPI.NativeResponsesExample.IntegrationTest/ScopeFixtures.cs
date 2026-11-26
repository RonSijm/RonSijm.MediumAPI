using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.NativeResponsesExample.IntegrationTest;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class UnrelatedThrowingAttribute : Attribute
{
    public UnrelatedThrowingAttribute()
    {
        throw new InvalidOperationException("An unrelated declaration must never be constructed.");
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class DerivedThrowingResponseAttribute : ProducesResponseTypeAttribute
{
    public DerivedThrowingResponseAttribute() : base(418)
    {
        throw new InvalidOperationException("A non-opted-in response subclass must never be constructed.");
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class DerivedThrowingGenericResponseAttribute : ProducesResponseTypeAttribute<DeclaredFailure>
{
    public DerivedThrowingGenericResponseAttribute() : base(418)
    {
        throw new InvalidOperationException("A non-opted-in generic response subclass must never be constructed.");
    }
}

[ProducesResponseType<DeclaredFailure>(499)]
public abstract class InheritedEndpointBase
{
    public Ok<CatalogItem> HandleAsync([FromServices] ResponseProbe probe)
    {
        return probe.Reply(7, "inherited");
    }
}

[UnrelatedThrowing]
[DerivedThrowingResponse]
[DerivedThrowingGenericResponse]
public sealed class IgnoredResponseEndpoint : InheritedEndpointBase, IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/ignored";
}

[ProducesResponseType<DeclaredFailure>(498)]
public abstract class InheritedGroupBase { }

[ProducesResponseType<DeclaredFailure>(497)]
public abstract class InheritedFilterBase : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return next(context);
    }
}

[UnrelatedThrowing]
[DerivedThrowingResponse]
[DerivedThrowingGenericResponse]
public sealed class IgnoredResponseFilter : InheritedFilterBase { }

[CatalogResponse(454, Label = "filter-field")]
[ProducesResponseType<DeclaredFailure>(455)]
public sealed class CustomResponseFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return next(context);
    }
}