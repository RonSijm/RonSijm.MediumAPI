using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AsParametersExample;

public static class BoundReadMetadata
{
    public const string Tag = "Scoped reads";
    public const string Pattern = "/customers/{customerId:long}/projects/{projectId:long}/versions/{versionId:long}/runs/{runId:long}";
}

public sealed class BoundRunRoute
{
    public BoundRunRoute() { }

    [FromRoute(Name = "customerId")]
    public long CustomerId { get; set; }

    [FromRoute(Name = "projectId")]
    public long ProjectId { get; set; }

    [FromRoute(Name = "versionId")]
    public long VersionId { get; set; }

    [FromRoute(Name = "runId")]
    public long RunId { get; set; }
}

public sealed record BoundDetail(long CustomerId, long ProjectId, long VersionId, long RunId);

public sealed record BoundVerification(long CustomerId, long ProjectId, long RunId);

public sealed record BoundReadCall(string Operation, long CustomerId, long? ProjectId, long RunId, int ArgumentCount, bool RequestCancellationToken);

public sealed class BoundReadOptions
{
    public long ProjectId { get; set; } = 11;
    public long VersionId { get; set; } = 13;
    public bool Missing { get; set; }
}

public sealed class BoundReadProbe
{
    private readonly ConcurrentQueue<BoundReadCall> calls = new();
    public IReadOnlyList<BoundReadCall> Calls => calls.ToArray();

    public void Record(string operation, long customerId, long? projectId, long runId, HttpContext context, CancellationToken token)
    {
        calls.Enqueue(new BoundReadCall(operation, customerId, projectId, runId, AggregateArgumentFilter.GetArgumentCount(context), token == context.RequestAborted));
    }
}

public sealed class BoundDetailService(BoundReadOptions options, BoundReadProbe probe, IHttpContextAccessor accessor)
{
    public Task<BoundDetail> GetAsync(long customerId, long runId, CancellationToken token)
    {
        probe.Record("detail", customerId, null, runId, accessor.HttpContext!, token);
        if (options.Missing) { throw new BoundReadNotFoundException(); }

        return Task.FromResult(new BoundDetail(customerId, options.ProjectId, options.VersionId, runId));
    }
}

public sealed class BoundVerificationService(BoundReadProbe probe, IHttpContextAccessor accessor)
{
    public Task<BoundVerification> GetAsync(long customerId, long projectId, long runId, CancellationToken token)
    {
        probe.Record("verification", customerId, projectId, runId, accessor.HttpContext!, token);
        return Task.FromResult(new BoundVerification(customerId, projectId, runId));
    }
}

public sealed class BoundReadNotFoundException : Exception { }

public sealed class BoundReadAccessResponsesAttribute : EndpointResponsesAttribute
{
    public BoundReadAccessResponsesAttribute() : base(401, 403) { }
}

[BoundReadAccessResponses]
[EndpointResponse(404)]
public sealed class BoundReadExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (BoundReadNotFoundException) { return TypedResults.NotFound(); }
    }
}

public static class BoundReadScenario
{
    public static void Map(WebApplication app)
    {
        app.TryMapMediumApiNamespace<BoundReadRoutes.BoundReadGroupDefinition>();
    }
}