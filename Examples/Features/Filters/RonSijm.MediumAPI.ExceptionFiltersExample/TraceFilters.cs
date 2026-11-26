namespace RonSijm.MediumAPI.ExceptionFiltersExample;

public sealed class RequestTrace
{
    public Guid ScopeId { get; } = Guid.NewGuid();
    public List<string> Entries { get; } = [];
}

public sealed record PipelineResponse(Guid ScopeId, IReadOnlyList<string> Trace);

public sealed record FilterDeclaration(string Name);

public abstract class TraceFilterBase(RequestTrace trace, string name) : IEndpointFilter
{
    protected static void Declare(IEndpointConventionBuilder endpoint, string name) => endpoint.WithMetadata(new FilterDeclaration(name));

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        trace.Entries.Add(name + ":enter");
        try { return await next(context); }
        finally { trace.Entries.Add(name + ":exit"); }
    }
}

public sealed class FluentGroupTraceFilter(RequestTrace trace) : TraceFilterBase(trace, "fluent-group"), IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint) => Declare(endpoint, "fluent-group");
}

public sealed class DeclaredGroupTraceFilter(RequestTrace trace) : TraceFilterBase(trace, "declared-group"), IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint) => Declare(endpoint, "declared-group");
}

public sealed class DeclaredClassTraceFilter(RequestTrace trace) : TraceFilterBase(trace, "class"), IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint) => Declare(endpoint, "class");
}

public sealed class DeclaredMethodTraceFilter(RequestTrace trace) : TraceFilterBase(trace, "method"), IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint) => Declare(endpoint, "method");
}

public sealed class FluentEndpointTraceFilter(RequestTrace trace) : TraceFilterBase(trace, "fluent-endpoint"), IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint) => Declare(endpoint, "fluent-endpoint");
}

public sealed class MustStayDisabledFilter : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint) => throw new InvalidOperationException("This disabled or inherited-class declaration must not be configured.");

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) => throw new InvalidOperationException("This disabled filter must not be invoked.");
}