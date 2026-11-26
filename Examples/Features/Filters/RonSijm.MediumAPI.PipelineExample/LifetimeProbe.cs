using System.Collections.Concurrent;
using System.Reflection;

namespace RonSijm.MediumAPI.PipelineExample;

/// <summary>Synthetic observations for the example's HTTP tests; contains no request payloads.</summary>
public sealed class LifetimeProbe
{
    private int activations;
    private int handlerCalls;
    private int dependencyDisposals;
    public int Activations => Volatile.Read(ref activations);
    public int HandlerCalls => Volatile.Read(ref handlerCalls);
    public int DependencyDisposals => Volatile.Read(ref dependencyDisposals);
    public ConcurrentDictionary<int, int> Disposals { get; } = new();
    public ConcurrentQueue<string> Events { get; } = new();
    public ConcurrentQueue<MethodInfo> FilterMethods { get; } = new();
    public TaskCompletionSource HandlerEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ResponseEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource DependencyDisposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Created() => Interlocked.Increment(ref activations);

    public void HandlerStarted()
    {
        Interlocked.Increment(ref handlerCalls);
        Events.Enqueue("handler-start");
        HandlerEntered.TrySetResult();
    }

    public void EndpointDisposed(int instanceId, bool asynchronous)
    {
        Disposals.AddOrUpdate(instanceId, 1, (_, count) => count + 1);
        Events.Enqueue(asynchronous ? "async-dispose" : "sync-dispose");
        Disposed.TrySetResult();
    }

    public void ScopeDisposed()
    {
        Interlocked.Increment(ref dependencyDisposals);
        Events.Enqueue("scope-dispose");
        DependencyDisposed.TrySetResult();
    }
}

public sealed class RequestDependency(LifetimeProbe probe) : IDisposable
{
    public Guid Id { get; } = Guid.NewGuid();
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
        probe.ScopeDisposed();
    }
}

public sealed record PipelineResponse(int TenantId, int Value, int InstanceId, Guid DependencyId);

public sealed record PipelineRequest(string Name);

public sealed class ExampleFailureException : Exception;