using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ScopedCommandsExample;

public sealed class CommandTrace
{
    public Guid ScopeId { get; } = Guid.NewGuid();
    public List<string> Entries { get; } = [];
}

public sealed class CommandProbe
{
    private int scopeCalls;
    private int operationCalls;

    public int ScopeCalls => Volatile.Read(ref scopeCalls);
    public int OperationCalls => Volatile.Read(ref operationCalls);

    public void VisitScope()
    {
        Interlocked.Increment(ref scopeCalls);
    }

    public void VisitOperation()
    {
        Interlocked.Increment(ref operationCalls);
    }
}

public sealed class OwnershipPause(bool enabled = false)
{
    public bool Enabled { get; } = enabled;
    public TaskCompletionSource<bool> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<bool> Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public sealed class ResourceCommands(CommandTrace trace, CommandProbe probe, OwnershipPause pause)
{
    public Guid ScopeId => trace.ScopeId;

    public async Task<bool> BelongsToProjectAsync(int projectId, int resourceId, string? outcome, CancellationToken cancellationToken)
    {
        probe.VisitScope();
        trace.Entries.Add("scope:enter");
        try
        {
            await Task.Yield();
            if (pause.Enabled)
            {
                pause.Entered.TrySetResult(true);
                await pause.Continue.Task.WaitAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfRequested(outcome, cancellationToken);
            return outcome != "deny" && projectId > 0 && resourceId > 0;
        }
        finally
        {
            trace.Entries.Add("scope:exit");
        }
    }

    public async Task<Ok<ResourceReceipt>> ReadAsync(int resourceId, string? outcome, CancellationToken cancellationToken)
    {
        await ExecuteAsync(outcome, cancellationToken);
        return TypedResults.Ok(new ResourceReceipt(resourceId, "read"));
    }

    public async Task<Results<ProblemHttpResult, NoContent>> ReviewAsync(CommandRequest request, CancellationToken cancellationToken)
    {
        await ExecuteAsync(request.Outcome, cancellationToken);
        if (request.Outcome == "problem")
        {
            return CommandProblems.Problem(400, "review-invalid", "The review could not be completed.");
        }

        return TypedResults.NoContent();
    }

    public async Task<Ok<ResourceReceipt>> ReleaseAsync(int releaseId, CommandRequest request, CancellationToken cancellationToken)
    {
        await ExecuteAsync(request.Outcome, cancellationToken);
        return TypedResults.Ok(new ResourceReceipt(releaseId, "release"));
    }

    private async Task ExecuteAsync(string? outcome, CancellationToken cancellationToken)
    {
        probe.VisitOperation();
        trace.Entries.Add("operation:enter");
        try
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfRequested(outcome, cancellationToken);
        }
        finally
        {
            trace.Entries.Add("operation:exit");
        }
    }

    private static void ThrowIfRequested(string? outcome, CancellationToken cancellationToken)
    {
        switch (outcome)
        {
            case "forbidden": throw new AccessDeniedException();
            case "missing": throw new ResourceMissingException();
            case "validation": throw new CommandValidationException("invalid-command");
            case "domain": throw new CommandDomainException();
            case "conflict": throw new CommandConflictException();
            case "confirmation": throw new ConfirmationRequiredException();
            case "unknown": throw new UnexpectedCommandException();
            case "cancel": throw new OperationCanceledException("Synthetic cancellation.", cancellationToken);
        }
    }
}

public static class ScopedOperations
{
    // Authorization/ownership stays in the application operation, before its callback.
    public static async Task<Results<TSuccess, NotFound>> InProjectAsync<TSuccess>(ResourceCommands commands, int projectId, int resourceId, string? scope, CancellationToken cancellationToken, Func<Task<TSuccess>> operation) where TSuccess : IResult
    {
        if (!await commands.BelongsToProjectAsync(projectId, resourceId, scope, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        return await operation();
    }
}