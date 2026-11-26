using System.Collections.Concurrent;

namespace RonSijm.MediumAPI.EndpointDocumentationExample;

public sealed record SharedQueryInput(string Search, long? CustomerId, bool UnscopedOnly, string? Action, string? EntityType, long? UserId, DateOnly? FromDate, DateOnly? ToDate, int Page, int PageSize, long? ProjectId, string? Kind);

public sealed record SharedQueryReply(string Kind, SharedQueryInput Query);

public sealed record SharedQueryCall(string Kind, SharedQueryInput Query, CancellationToken Token);

public sealed class SharedQueryProbe
{
    private readonly ConcurrentQueue<SharedQueryCall> calls = new();
    private readonly ConcurrentQueue<string> events = new();
    private readonly ConcurrentQueue<CancellationToken> handlerTokens = new();
    private readonly ConcurrentQueue<object?[]> invocations = new();
    public IReadOnlyList<SharedQueryCall> Calls => calls.ToArray();
    public IReadOnlyList<string> Events => events.ToArray();
    public IReadOnlyList<CancellationToken> HandlerTokens => handlerTokens.ToArray();
    public IReadOnlyList<object?[]> Invocations => invocations.ToArray();
    public CancellationToken? RequestToken { get; private set; }

    public void RecordFilter(CancellationToken token, object?[] arguments)
    {
        RequestToken = token;
        invocations.Enqueue(arguments);
        events.Enqueue("filter-enter");
    }

    public void RecordHandler(CancellationToken token)
    {
        handlerTokens.Enqueue(token);
        events.Enqueue("handler");
    }

    public void RecordService(SharedQueryCall call)
    {
        calls.Enqueue(call);
        events.Enqueue("service:" + call.Kind);
    }

    public void RecordTranslation()
    {
        events.Enqueue("translated");
    }
}

public static class SharedQueryNormalization
{
    public static SharedQueryInput Create(string? search, long? customerId, bool? platform, string? action, string? entityType, long? userId, string? from, string? to, int? page, int? pageSize, long? projectId, string? kind)
    {
        static DateOnly? ParseDay(string? value)
        {
            return DateOnly.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var day) ? day : null;
        }

        return new SharedQueryInput(Search: search ?? string.Empty, CustomerId: customerId, UnscopedOnly: platform == true, Action: action, EntityType: entityType, UserId: userId, FromDate: ParseDay(from), ToDate: ParseDay(to), Page: page ?? 1, PageSize: pageSize ?? 10, ProjectId: projectId, Kind: kind);
    }
}

public sealed class SharedQueryValidationException : Exception
{
    public SharedQueryValidationException() : base("Synthetic shared query validation.") { }
}

public sealed class SharedQueryService
{
    public SharedQueryService(SharedQueryProbe probe)
    {
        Probe = probe;
    }

    public SharedQueryProbe Probe { get; }
    public IReadOnlyList<SharedQueryCall> Calls => Probe.Calls;

    public Task<SharedQueryReply> SearchAsync(SharedQueryInput query, CancellationToken cancellationToken)
    {
        return InvokeAsync("search", query, cancellationToken);
    }

    public Task<SharedQueryReply> LoadWorkspaceAsync(SharedQueryInput query, CancellationToken cancellationToken)
    {
        return InvokeAsync("workspace", query, cancellationToken);
    }

    private Task<SharedQueryReply> InvokeAsync(string kind, SharedQueryInput query, CancellationToken cancellationToken)
    {
        Probe.RecordService(new SharedQueryCall(kind, query, cancellationToken));
        if (query.Search == "validation") { throw new SharedQueryValidationException(); }

        return Task.FromResult(new SharedQueryReply(kind, query));
    }
}

// This adapts one deliberate synthetic failure; it does not add validation or authorization policy.
public sealed class SharedQueryValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var probe = context.HttpContext.RequestServices.GetRequiredService<SharedQueryProbe>();
        probe.RecordFilter(context.HttpContext.RequestAborted, context.Arguments.ToArray());
        try { return await next(context); }
        catch (SharedQueryValidationException)
        {
            probe.RecordTranslation();
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Check the synthetic query.", extensions: new Dictionary<string, object?> { ["code"] = "synthetic-query" });
        }
    }
}

public static class SharedQuerySample
{
    public static void Map(WebApplication app)
    {
        app.TryMapMediumApiNamespace<SharedQueryRoutes.SharedQueryGroupDefinition>();
    }
}