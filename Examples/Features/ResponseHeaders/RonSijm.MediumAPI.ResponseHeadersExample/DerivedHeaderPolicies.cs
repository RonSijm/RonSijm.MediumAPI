using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ResponseHeadersExample;

public sealed class SampleNoStoreAttribute : ResponseHeaderAttribute
{
    public SampleNoStoreAttribute() : base("CacheControl", "no-store") { }
}

public sealed class SampleNoCachePragmaAttribute : ResponseHeaderAttribute
{
    public SampleNoCachePragmaAttribute() : base("Pragma", "no-cache") { }
}

public sealed class SampleHeaderAttribute : ResponseHeaderAttribute
{
    public SampleHeaderAttribute(string name, params string[] values) : base(name, values) { }
}

public sealed class SampleSelectedHeaderAttribute : ResponseHeaderAttribute
{
    public SampleSelectedHeaderAttribute(Type resultType) : base("X-Constructor-Selected", "selected")
    {
        ResultType = resultType;
    }
}

public sealed record DerivedHeaderReply(string Kind);

public sealed class DerivedHeaderProbe
{
    public int Calls { get; set; }
    public object? Returned { get; set; }
    public object? Observed { get; set; }
}

public static class DerivedHeaderBodies
{
    public static Results<Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>>, NotFound> Create(string kind, DerivedHeaderProbe probe)
    {
        probe.Calls++;
        if (kind == "fault") { throw new InvalidOperationException("Synthetic header example failure."); }

        if (kind == "cancel") { throw new OperationCanceledException("Synthetic header example cancellation."); }

        Results<Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>>, NotFound> result;
        if (kind == "ok")
        {
            Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>> success = TypedResults.Ok(new DerivedHeaderReply("ok"));
            result = success;
        }
        else if (kind == "created")
        {
            Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>> success = TypedResults.Created("/derived/items/7", new DerivedHeaderReply("created"));
            result = success;
        }
        else { result = TypedResults.NotFound(); }

        probe.Returned = result;
        return result;
    }
}

public sealed class DerivedHeaderObserver : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        context.HttpContext.RequestServices.GetRequiredService<DerivedHeaderProbe>().Observed = result;
        return result;
    }
}

public sealed class DerivedHeaderGuard : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return context.HttpContext.Request.Headers.ContainsKey("X-Sample-Reject") ? ValueTask.FromResult<object?>(TypedResults.StatusCode(403)) : next(context);
    }
}

[SampleHeader("X-Ignored-Filter-Base", "ignored")]
public abstract class DerivedHeaderFilterBase;

[SampleHeader("X-Filter", "filtered")]
[SampleNoStore(ResultTypes = new[] { typeof(Ok<>), typeof(Created<>) })]
public sealed class DerivedHeaderFilter : DerivedHeaderFilterBase, IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return context.HttpContext.Request.Query.ContainsKey("filterBlock") ? ValueTask.FromResult<object?>(TypedResults.Created("/derived/items/blocked", new DerivedHeaderReply("blocked"))) : next(context);
    }
}

[SampleHeader("X-Factory", "factory")]
[SampleNoCachePragma]
public sealed class DerivedHeaderFactory : IMediumApiEndpointFilterFactory
{
    private DerivedHeaderFactory()
    {
        throw new InvalidOperationException("A static factory is never instantiated.");
    }

    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        return next;
    }
}

public readonly struct DerivedHeaderConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        builder.WithResponseHeader(new SampleNoCachePragmaAttribute());
        builder.WithResponseHeader(new SampleHeaderAttribute("X-Convention", "startup"));
    }
}

public static class DerivedHeaderSample
{
    public static void Map(WebApplication app)
    {
        var group = app.TryMapMediumApiNamespace<DerivedPolicies.DerivedHeaderGroupDefinition>();
        if (group is null) { return; }

        group.MapMediumApiEndpoint<DerivedPolicies.DerivedEntryEndpoint>("/alias/{kind}").WithoutDefaultEndpointName();
        group.MapGet("/fluent", () => TypedResults.Ok(new DerivedHeaderReply("fluent"))).WithName("DerivedFluent").WithResponseHeader(new SampleHeaderAttribute("X-Fluent", "first", "second")).WithResponseHeader(new SampleNoStoreAttribute { ResultType = typeof(Ok<>) });
        group.MapGet("/convention", () => TypedResults.Ok(new DerivedHeaderReply("convention"))).WithName("DerivedConvention").AddMediumApiConvention<DerivedHeaderConvention>();
    }
}