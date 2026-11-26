using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ProviderPoliciesExample;

public sealed record ProviderRequest(string? Outcome);

public sealed record ProviderReceipt(int Id, string State);

public abstract class SampleApplicationException : Exception
{
    protected SampleApplicationException() : base("Internal synthetic detail must not be returned.") { }
}

public sealed class RemoteProviderException : SampleApplicationException { }

public sealed class LocalProviderException : Exception
{
    public LocalProviderException() : base("Internal synthetic detail must not be returned.") { }
}

public sealed class ProviderConfigurationException : SampleApplicationException { }

public sealed class AccessDeniedException : SampleApplicationException { }

public sealed class ResourceMissingException : SampleApplicationException { }

public sealed class CommandConflictException : SampleApplicationException { }

public sealed class CommandValidationException : SampleApplicationException { }

public sealed class UnexpectedProviderException : Exception { }

public sealed class PolicyTrace
{
    public Guid ScopeId { get; } = Guid.NewGuid();
    public List<string> Entries { get; } = [];
}

public sealed class ProviderProbe
{
    private int calls;
    public int Calls => Volatile.Read(ref calls);

    public void Visit()
    {
        Interlocked.Increment(ref calls);
    }
}

public static class ProviderProblems
{
    public static ProblemHttpResult Remote()
    {
        return Problem(502, "remote-state-unconfirmed", "Remote state could not be confirmed.");
    }

    public static ProblemHttpResult Local()
    {
        return Problem(400, "local-provider-unavailable", "Local provider tools are unavailable.");
    }

    public static ProblemHttpResult Problem(int status, string code, string title)
    {
        return TypedResults.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code });
    }
}

public sealed class ProviderOperations(PolicyTrace trace, ProviderProbe probe)
{
    public Guid ScopeId => trace.ScopeId;

    public async Task<ProviderReceipt> RemoteAsync(int id, ProviderRequest request, CancellationToken cancellationToken)
    {
        await ExecuteAsync(request.Outcome, remote: true, cancellationToken);
        return new ProviderReceipt(id, "remote-ready");
    }

    public async Task<ProviderReceipt> LocalAsync(int id, ProviderRequest request, CancellationToken cancellationToken)
    {
        await ExecuteAsync(request.Outcome, remote: false, cancellationToken);
        return new ProviderReceipt(id, "local-ready");
    }

    public async Task<bool> OpenAsync(int id, ProviderRequest request, CancellationToken cancellationToken)
    {
        await ExecuteAsync(request.Outcome, remote: false, cancellationToken);
        return id > 0;
    }

    private async Task ExecuteAsync(string? outcome, bool remote, CancellationToken cancellationToken)
    {
        probe.Visit();
        trace.Entries.Add("operation:enter");
        try
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            switch (outcome)
            {
                case "provider" when remote: throw new RemoteProviderException();
                case "provider": throw new LocalProviderException();
                case "configuration": throw new ProviderConfigurationException();
                case "forbidden": throw new AccessDeniedException();
                case "missing": throw new ResourceMissingException();
                case "validation": throw new CommandValidationException();
                case "conflict": throw new CommandConflictException();
                case "unknown": throw new UnexpectedProviderException();
                case "cancel": throw new OperationCanceledException("Synthetic cancellation.", cancellationToken);
            }
        }
        finally
        {
            trace.Entries.Add("operation:exit");
        }
    }
}