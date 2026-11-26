namespace RonSijm.MediumAPI.NativeFiltersExample;

public abstract class NativeTraceFilterBase(RequestTrace trace, string name) : IEndpointFilter
{
    protected RequestTrace Trace => trace;

    public virtual async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        trace.Entries.Add(name + ":enter");
        try
        {
            return await next(context);
        }
        finally
        {
            trace.Entries.Add(name + ":exit");
        }
    }
}

[ResponseHeader("X-Native-Group", "present")]
public sealed class FluentGroupTraceFilter(RequestTrace trace) : NativeTraceFilterBase(trace, "fluent-group") { }

public sealed class NamespaceTraceFilter(RequestTrace trace) : NativeTraceFilterBase(trace, "namespace")
{
    public override ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.Response.Headers["X-Native-Scope"] = Trace.ScopeId.ToString("D");
        return base.InvokeAsync(context, next);
    }
}

[ResponseHeader("X-Native-Class", "present")]
public sealed class ClassTraceFilter(RequestTrace trace) : NativeTraceFilterBase(trace, "class")
{
    // Matching a method name alone does not opt a native filter into startup conventions.
    public static void Configure(IEndpointConventionBuilder endpoint) => throw new InvalidOperationException("An ordinary native filter has no MediumAPI startup convention.");
}

public sealed class MethodValueFilter(RequestTrace trace) : NativeTraceFilterBase(trace, "method")
{
    public override ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.Arguments[0] = context.GetArgument<int>(0) + 10;
        return base.InvokeAsync(context, next);
    }
}

[ResponseHeader("X-Native-Fluent", "present")]
public sealed class FluentEndpointTraceFilter(RequestTrace trace) : NativeTraceFilterBase(trace, "fluent-endpoint") { }

[ResponseHeader("X-Native-Configured", "present")]
public sealed class ConfiguredTraceFilter(RequestTrace trace) : NativeTraceFilterBase(trace, "configured"), IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint) => endpoint.WithMetadata(new FilterDeclaration("configured"));
}