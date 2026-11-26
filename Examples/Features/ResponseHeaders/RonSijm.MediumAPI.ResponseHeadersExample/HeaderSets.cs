using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ResponseHeadersExample;

public sealed class SampleNoCacheHeadersAttribute : ResponseHeadersAttribute
{
    public SampleNoCacheHeadersAttribute() : base(new ResponseHeaderAttribute[] { new SampleNoStoreAttribute(), new SampleNoCachePragmaAttribute() }) { }
}

public sealed class SampleSelectedCacheHeadersAttribute : ResponseHeadersAttribute
{
    public SampleSelectedCacheHeadersAttribute() : base(new ResponseHeaderAttribute[] { new SampleNoStoreAttribute { ResultTypes = new[] { typeof(Ok<>), typeof(StatusCodeHttpResult), typeof(FileContentHttpResult) } }, new SampleNoCachePragmaAttribute { ResultTypes = new[] { typeof(Ok<>), typeof(StatusCodeHttpResult), typeof(FileContentHttpResult) } } }) { }
}

public sealed class SamplePairHeadersAttribute : ResponseHeadersAttribute
{
    public SamplePairHeadersAttribute(string prefix, string value) : base(new ResponseHeaderAttribute[] { new SampleHeaderAttribute(prefix + "-First", value), new SampleHeaderAttribute(prefix + "-Second", value) }) { }
}

public sealed record HeaderSetReply(string Kind);

public sealed class HeaderSetProbe
{
    public int Calls { get; set; }
    public object? Returned { get; set; }
    public object? Observed { get; set; }
}

public static class HeaderSetBodies
{
    public static Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> Create(string kind, HeaderSetProbe probe)
    {
        probe.Calls++;
        if (kind == "fault") { throw new InvalidOperationException("Synthetic header-set failure."); }

        if (kind == "cancel") { throw new OperationCanceledException("Synthetic header-set cancellation."); }

        Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> result;
        if (kind == "ok") { result = TypedResults.Ok(new HeaderSetReply(kind)); }
        else if (kind == "notmodified") { result = TypedResults.StatusCode(StatusCodes.Status304NotModified); }
        else if (kind == "file") { result = TypedResults.File(new byte[] { 1, 2, 3 }, "application/octet-stream", "sample.bin"); }
        else { result = TypedResults.NotFound(); }

        probe.Returned = result;
        return result;
    }

    public static Ok<HeaderSetReply> Fluent()
    {
        return TypedResults.Ok(new HeaderSetReply("fluent"));
    }
}

public sealed class HeaderSetObserver : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        context.HttpContext.RequestServices.GetRequiredService<HeaderSetProbe>().Observed = result;
        return result;
    }
}

public sealed class HeaderSetGuard : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return context.HttpContext.Request.Headers.ContainsKey("X-Reject") ? ValueTask.FromResult<object?>(TypedResults.StatusCode(StatusCodes.Status403Forbidden)) : next(context);
    }
}

[SampleNoCacheHeaders]
[UnrelatedHeaderExample]
public sealed class GroupedHeaderFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return context.HttpContext.Request.Query.ContainsKey("block") ? ValueTask.FromResult<object?>(TypedResults.StatusCode(StatusCodes.Status304NotModified)) : next(context);
    }
}

[SampleNoCacheHeaders]
[UnrelatedHeaderExample]
public sealed class GroupedHeaderFactory : IMediumApiEndpointFilterFactory
{
    private GroupedHeaderFactory()
    {
        throw new InvalidOperationException("A static header-set factory must not be activated.");
    }

    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        return next;
    }
}

public static class HeaderSetsSample
{
    public static void Map(WebApplication app)
    {
        var group = app.TryMapMediumApiNamespace<SetPolicies.HeaderSetsGroupDefinition>();
        if (group is null) { return; }

        group.MapMediumApiEndpoint<SetPolicies.HeaderSetEntryEndpoint>("/alias/{kind}").WithName("HeaderSetAlias");
        group.MapGet("/fluent", HeaderSetBodies.Fluent).WithName("HeaderSetFluent").WithResponseHeaders(new SampleNoCacheHeadersAttribute());
    }
}