using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;

namespace RonSijm.MediumAPI.ServiceEndpointsExample;

public sealed record ServiceReceipt(string Kind, int Value, int InstanceId, int DependencyId, string Origin, bool SameScope, bool Disposed);

public sealed class ServiceFailureException : Exception;

public sealed class ServiceProbe
{
    private int sequence;
    private int dependencySequence;
    private readonly ConcurrentDictionary<int, TaskCompletionSource> disposed = new();
    public ConcurrentDictionary<int, string> Creations { get; } = new();
    public ConcurrentDictionary<int, int> Disposals { get; } = new();
    public ConcurrentDictionary<int, int> Calls { get; } = new();
    public ConcurrentQueue<MethodInfo> FilterMethods { get; } = new();
    public ConcurrentQueue<string> Events { get; } = new();
    public TaskCompletionSource HandlerEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ResultEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseResult { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int Created(string kind)
    {
        var id = Interlocked.Increment(ref sequence);
        Creations[id] = kind;
        return id;
    }

    public int NewDependency() => Interlocked.Increment(ref dependencySequence);
    public void Called(int id) => Calls.AddOrUpdate(id, 1, (_, count) => count + 1);
    public Task WhenDisposed(int id) => disposed.GetOrAdd(id, _ => new(TaskCreationOptions.RunContinuationsAsynchronously)).Task;

    public void Disposed(int id)
    {
        Disposals.AddOrUpdate(id, 1, (_, count) => count + 1);
        Events.Enqueue("dispose:" + id);
        disposed.GetOrAdd(id, _ => new(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult();
    }
}

public sealed class RequestDependency(ServiceProbe probe)
{
    public int Id { get; } = probe.NewDependency();
}

public abstract class ServiceTarget(ServiceProbe probe, string kind, int dependencyId, string origin)
{
    protected ServiceProbe Probe { get; } = probe;
    public int Id { get; } = probe.Created(kind);
    public int DependencyId { get; } = dependencyId;
    public bool IsDisposed { get; private set; }

    public ServiceReceipt Receipt(int value, bool sameScope)
    {
        if (IsDisposed)
        {
            throw new InvalidOperationException("A disposed endpoint cannot execute a request.");
        }

        Probe.Called(Id);
        return new(kind, value, Id, DependencyId, origin, sameScope, IsDisposed);
    }

    protected void DisposeTarget()
    {
        IsDisposed = true;
        Probe.Disposed(Id);
    }
}

public sealed class ServiceDeferredResult(ServiceTarget owner, ServiceProbe probe, ServiceReceipt receipt, bool hold) : IResult, IEndpointMetadataProvider
{
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) => builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(ServiceReceipt), ["application/json"]));

    public async Task ExecuteAsync(HttpContext context)
    {
        probe.ResultEntered.TrySetResult();
        if (hold)
        {
            await probe.ReleaseResult.Task.WaitAsync(context.RequestAborted);
        }

        if (owner.IsDisposed)
        {
            throw new InvalidOperationException("The endpoint must remain alive while the result executes.");
        }

        if (receipt.Value == -3)
        {
            throw new ServiceFailureException();
        }

        await context.Response.WriteAsJsonAsync(receipt, context.RequestAborted);
        probe.Events.Enqueue("result-complete:" + owner.Id);
    }
}