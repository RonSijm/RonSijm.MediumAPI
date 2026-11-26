using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.PipelineExample.Endpoints;

public sealed class FailureEndpoint(LifetimeProbe probe, RequestDependency dependency) : OwnedEndpoint(probe, dependency), IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/failure/{value:int}";

    public IResult HandleAsync([FromRoute] int value, [FromRoute] int tenantId)
    {
        _ = Respond(tenantId, value);
        throw new ExampleFailureException();
    }
}

public sealed class AsyncFailureEndpoint(LifetimeProbe probe, RequestDependency dependency) : AsyncOwnedEndpoint(probe, dependency), IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/async-failure/{value:int}";

    public async ValueTask<IResult> HandleAsync([FromRoute] int value, [FromRoute] int tenantId)
    {
        await Task.Yield();
        _ = Respond(tenantId, value);
        throw new ExampleFailureException();
    }
}

public sealed class CancellationEndpoint(LifetimeProbe probe, RequestDependency dependency) : AsyncOwnedEndpoint(probe, dependency), IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/cancellation/{value:int}";

    public async Task<IResult> HandleAsync([FromRoute] int value, [FromRoute] int tenantId, CancellationToken cancellationToken)
    {
        Probe.HandlerStarted();
        try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); }
        finally { Probe.Events.Enqueue("handler-complete"); }

        return Results.Ok(Respond(tenantId, value));
    }
}

public sealed class BodyEndpoint(LifetimeProbe probe, RequestDependency dependency) : OwnedEndpoint(probe, dependency), IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/body/{value:int}";

    public IResult HandleAsync([FromRoute] int value, [FromRoute] int tenantId, [FromBody] PipelineRequest request) => Results.Ok(Respond(tenantId, value));
}

public sealed class TaskWithoutResultEndpoint(LifetimeProbe probe, RequestDependency dependency) : OwnedEndpoint(probe, dependency), IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/task-empty/{value:int}";

    public async Task HandleAsync([FromRoute] int value, [FromRoute] int tenantId)
    {
        await Task.Yield();
        _ = Respond(tenantId, value);
    }
}

public sealed class ValueTaskWithoutResultEndpoint(LifetimeProbe probe, RequestDependency dependency) : AsyncOwnedEndpoint(probe, dependency), IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value-task-empty/{value:int}";

    public async ValueTask HandleAsync([FromRoute] int value, [FromRoute] int tenantId)
    {
        await Task.Yield();
        _ = Respond(tenantId, value);
    }
}

public sealed class DeferredResultEndpoint(LifetimeProbe probe, RequestDependency dependency) : AsyncOwnedEndpoint(probe, dependency), IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/deferred/{value:int}";

    public IResult HandleAsync([FromRoute] int value, [FromRoute] int tenantId, [FromQuery] bool hold = false) => new DeferredResult(this, Probe, Respond(tenantId, value), hold);

    private sealed class DeferredResult(OwnedEndpoint owner, LifetimeProbe probe, PipelineResponse response, bool hold) : IResult
    {
        public async Task ExecuteAsync(HttpContext context)
        {
            probe.ResponseEntered.TrySetResult();
            if (hold)
            {
                await probe.Release.Task.WaitAsync(context.RequestAborted);
            }

            if (owner.IsDisposed)
            {
                throw new InvalidOperationException("The endpoint must remain alive while its result executes.");
            }

            await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
            probe.Events.Enqueue("result-complete");
        }
    }
}