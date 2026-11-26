namespace RonSijm.MediumAPI.PipelineExample.Endpoints;

public abstract class OwnedEndpoint(LifetimeProbe probe, RequestDependency dependency) : IDisposable
{
    protected LifetimeProbe Probe { get; } = probe;
    protected RequestDependency Dependency { get; } = dependency;
    public int InstanceId { get; } = probe.Created();
    public bool IsDisposed { get; private set; }

    protected PipelineResponse Respond(int tenantId, int value)
    {
        if (Dependency.IsDisposed || IsDisposed)
        {
            throw new InvalidOperationException("The sample resource was disposed before handler completion.");
        }

        Probe.HandlerStarted();
        Probe.Events.Enqueue("handler-complete");
        return new(tenantId, value, InstanceId, Dependency.Id);
    }

    public void Dispose() => RecordDisposal(asynchronous: false);

    protected void RecordDisposal(bool asynchronous)
    {
        IsDisposed = true;
        Probe.EndpointDisposed(InstanceId, asynchronous);
    }
}

public abstract class AsyncOwnedEndpoint(LifetimeProbe probe, RequestDependency dependency) : OwnedEndpoint(probe, dependency), IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
        RecordDisposal(asynchronous: true);
    }
}