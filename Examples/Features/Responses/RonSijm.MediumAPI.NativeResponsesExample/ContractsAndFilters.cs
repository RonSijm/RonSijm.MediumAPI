using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.NativeResponsesExample;

public sealed record CatalogItem(int Id, string Source);

public sealed record MissingItem(string Code);

public sealed record DeclaredFailure(string Message);

public sealed record GroupNotice(string Message);

public sealed record ConfiguredNotice(string Message);

public sealed record FallbackNotice(string Message);

public sealed class ResponseProbe
{
    private int handlerCalls;
    private int filterCalls;

    public int HandlerCalls => Volatile.Read(ref handlerCalls);
    public int FilterCalls => Volatile.Read(ref filterCalls);

    public Ok<CatalogItem> Reply(int id, string source)
    {
        Interlocked.Increment(ref handlerCalls);
        return TypedResults.Ok(new CatalogItem(id, source));
    }

    public void VisitFilter()
    {
        Interlocked.Increment(ref filterCalls);
    }
}

[ProducesResponseType<DeclaredFailure>(404, "application/vnd.catalog-error+json")]
public sealed class MissingItemFilter(ResponseProbe probe) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        probe.VisitFilter();
        if (context.GetArgument<int>(0) <= 0)
        {
            return ValueTask.FromResult<object?>(TypedResults.NotFound(new MissingItem("missing")));
        }

        return next(context);
    }
}

[ProducesResponseType<DeclaredFailure>(425, "application/vnd.declared+json")]
public sealed class ConfiguredResponseFilter : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.WithMetadata(new ProducesResponseTypeAttribute<ConfiguredNotice>(425, "application/vnd.configured+json"));
        endpoint.WithResponseHeader("X-Native-Configured", "present");
    }

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return next(context);
    }
}

[EndpointResponseFallback(404, typeof(FallbackNotice))]
public sealed class MissingResponseFallbackFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return next(context);
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class CatalogResponseAttribute : ProducesResponseTypeAttribute, IEndpointMetadataAttribute
{
    public CatalogResponseAttribute(int statusCode) : base(typeof(DeclaredFailure), statusCode) { }

    public string Label = "default";
}