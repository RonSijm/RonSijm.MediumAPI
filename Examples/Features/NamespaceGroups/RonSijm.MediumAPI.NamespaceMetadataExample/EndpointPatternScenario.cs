using System.Collections.Concurrent;

namespace RonSijm.MediumAPI.NamespaceMetadataExample;

public static class EndpointPatternMetadata
{
    public const string Flag = "Sample:EndpointPatterns";
    public const string Tag = "Declared endpoint patterns";
    public const string Policy = "PatternRead";
    public const string Item = "/items/{itemId:long}";
    public const string Lookup = Item + "/lookup";
}

public sealed record EndpointPatternReply(long? CustomerId, long? ItemId, string Kind);

public sealed class EndpointPatternProbe
{
    private readonly ConcurrentQueue<string> filters = new();
    private readonly ConcurrentQueue<string> handlers = new();
    public IReadOnlyList<string> Filters => filters.ToArray();
    public IReadOnlyList<string> Handlers => handlers.ToArray();

    public void RecordFilter(string path)
    {
        filters.Enqueue(path);
    }

    public void RecordHandler(string kind)
    {
        handlers.Enqueue(kind);
    }
}

[EndpointResponse(StatusCodes.Status400BadRequest)]
public sealed class EndpointPatternFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.RequestServices.GetRequiredService<EndpointPatternProbe>().RecordFilter(context.HttpContext.Request.Path.ToString());
        if (context.HttpContext.Request.Headers["X-Reject"] == "true") { return TypedResults.BadRequest(); }

        return await next(context);
    }
}

public static class EndpointPatternScenario
{
    public static void Map(WebApplication app)
    {
        app.TryMapMediumApiNamespace<PatternRoutes.EndpointPatternGroupDefinition>();
    }
}