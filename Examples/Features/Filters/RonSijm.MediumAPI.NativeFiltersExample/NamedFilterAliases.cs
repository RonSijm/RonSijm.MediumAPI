namespace RonSijm.MediumAPI.NativeFiltersExample;

public sealed class NamedFilterProbe
{
    private int nextScope;
    public List<string> Constructors { get; } = new();
    public int HandlerCalls { get; set; }
    public int ArgumentCount { get; set; }
    public bool OriginalScopeArgument { get; set; }
    public object? Returned { get; set; }
    public object? Observed { get; set; }

    public int NextScope()
    {
        return Interlocked.Increment(ref nextScope);
    }
}

public sealed class NamedFilterScope
{
    public NamedFilterScope(NamedFilterProbe probe)
    {
        Id = probe.NextScope();
    }

    public int Id { get; }
    public List<string> Trace { get; } = new();
}

public sealed record NamedFilterReply(int Value, int? Page, int ScopeId, IReadOnlyList<string> Trace);

public sealed record NamedFilterError(string Code);

public sealed record NamedFilterConfiguredMetadata(string Value);

public sealed class NamedFilterMissingException : Exception
{
    public NamedFilterMissingException() : base("Synthetic named-filter missing resource.") { }
}

[EndpointResponse(StatusCodes.Status400BadRequest, typeof(NamedFilterError))]
public sealed class AliasGroupGuardFilter : IEndpointFilter
{
    private readonly NamedFilterScope scope;

    public AliasGroupGuardFilter(NamedFilterScope scope, NamedFilterProbe probe)
    {
        this.scope = scope;
        probe.Constructors.Add("group:" + scope.Id);
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        scope.Trace.Add("group:before");
        try
        {
            if (context.HttpContext.Request.Headers.ContainsKey("X-Reject")) { return TypedResults.BadRequest(new NamedFilterError("blocked")); }

            return await next(context);
        }
        finally { scope.Trace.Add("group:after"); }
    }
}

[EndpointResponse(StatusCodes.Status404NotFound)]
[ResponseHeader("X-Named-Configured", "registered")]
public sealed class AliasConfiguredReadFilter : IMediumApiEndpointFilter
{
    private readonly NamedFilterScope scope;

    public AliasConfiguredReadFilter(NamedFilterScope scope, NamedFilterProbe probe)
    {
        this.scope = scope;
        probe.Constructors.Add("configured:" + scope.Id);
    }

    public static void Configure(IEndpointConventionBuilder builder)
    {
        builder.WithMetadata(new NamedFilterConfiguredMetadata("configured"));
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        scope.Trace.Add("configured:before");
        try { return await next(context); }
        catch (NamedFilterMissingException) { return TypedResults.NotFound(); }
        finally { scope.Trace.Add("configured:after"); }
    }
}

public sealed class AliasArgumentFilter : IEndpointFilter
{
    private readonly NamedFilterScope scope;
    private readonly NamedFilterProbe probe;

    public AliasArgumentFilter(NamedFilterScope scope, NamedFilterProbe probe)
    {
        this.scope = scope;
        this.probe = probe;
        probe.Constructors.Add("argument:" + scope.Id);
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        scope.Trace.Add("argument:before");
        probe.ArgumentCount = context.Arguments.Count;
        probe.OriginalScopeArgument = ReferenceEquals(scope, context.GetArgument<NamedFilterScope>(2));
        context.Arguments[0] = context.GetArgument<int>(0) + 10;
        try
        {
            var result = await next(context);
            probe.Observed = result;
            return result;
        }
        finally { scope.Trace.Add("argument:after"); }
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class ThrowingFilterAliasDecorationAttribute : Attribute
{
    public ThrowingFilterAliasDecorationAttribute()
    {
        throw new InvalidOperationException("Alias decorations must not be imported.");
    }
}

public sealed class NamedGroupGuardAttribute : EndpointFilterAttribute<AliasGroupGuardFilter> { }

[EndpointResponse(418)]
[ResponseHeader("X-Ignored-Alias", "ignored")]
[ThrowingFilterAliasDecoration]
public sealed class NamedConfiguredReadAttribute : EndpointFilterAttribute<AliasConfiguredReadFilter> { }

public sealed class NamedArgumentAttribute : EndpointFilterAttribute<AliasArgumentFilter> { }

public static class NamedFilterBodies
{
    public static Microsoft.AspNetCore.Http.HttpResults.Ok<NamedFilterReply> Read(int value, int? page, NamedFilterScope scope, NamedFilterProbe probe, HttpContext context)
    {
        probe.HandlerCalls++;
        scope.Trace.Add("handler");
        if (context.Request.Query["outcome"] == "missing") { throw new NamedFilterMissingException(); }

        var result = TypedResults.Ok(new NamedFilterReply(value, page, scope.Id, scope.Trace.AsReadOnly()));
        probe.Returned = result;
        return result;
    }
}

public static class NamedFilterAliasScenario
{
    public static void Map(WebApplication app)
    {
        app.TryMapMediumApiNamespace<NamedFilterRoutes.NamedFilterGroupDefinition>();
    }
}