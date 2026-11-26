using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AntiforgeryOptOutExample.IntegrationTest.ScopeFixtures;

[DisableEndpointAntiforgery]
public abstract class MarkedEndpointBase : IPostEndpoint
{
    [StringSyntax("Route")]
    public abstract string Pattern { get; }

    [RequireAntiforgeryToken]
    public Ok<FormReceipt> HandleAsync([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);
}

public sealed class InheritedClassMarkerEndpoint : MarkedEndpointBase, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public override string Pattern => "/fixtures/class-inheritance";
}

public abstract class MarkedMethodBase : IPostEndpoint
{
    [StringSyntax("Route")]
    public abstract string Pattern { get; }

    [RequireAntiforgeryToken]
    [DisableEndpointAntiforgery]
    public virtual Ok<FormReceipt> HandleAsync([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);
}

public sealed class InheritedMethodMarkerEndpoint : MarkedMethodBase, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public override string Pattern => "/fixtures/method-inheritance";
}

public sealed class OverrideMethodMarkerEndpoint : MarkedMethodBase, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public override string Pattern => "/fixtures/method-override";

    public override Ok<FormReceipt> HandleAsync([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);
}

public sealed class PlainFormEndpoint : IPostEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/fixtures/plain";

    [RequireAntiforgeryToken]
    public Ok<FormReceipt> HandleAsync([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);
}

[DisableEndpointAntiforgery]
public abstract class MarkedFilterBase : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) => next(context);
}

public sealed class InheritedFilterMarker : MarkedFilterBase { }

[DisableEndpointAntiforgery]
[UnrelatedThrowing]
public sealed class UnrelatedAttributeEndpoint : IPostEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/fixtures/unrelated";

    public Ok<FormReceipt> HandleAsync([FromForm] string message, HttpContext context, FormProbe probe) => SampleReplies.Form(message, context, probe);
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class UnrelatedThrowingAttribute : Attribute
{
    public UnrelatedThrowingAttribute() => throw new InvalidOperationException("Unrelated class attributes must not be constructed.");
}