using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.InputPoliciesExample;

public static class CommonCommandPolicy
{
    public static async Task<Results<TSuccess, ForbidHttpResult, NotFound, ProblemHttpResult>> RunAsync<TSuccess>(Func<Task<TSuccess>> operation, InputTrace trace) where TSuccess : IResult
    {
        trace.Entries.Add("common:enter");
        try
        {
            return await operation();
        }
        catch (AccessDeniedException)
        {
            trace.Entries.Add("common:forbidden");
            return TypedResults.Forbid();
        }
        catch (ResourceMissingException)
        {
            trace.Entries.Add("common:missing");
            return TypedResults.NotFound();
        }
        catch (CommandConflictException)
        {
            trace.Entries.Add("common:conflict");
            return InputProblems.Problem(409, "command-conflict", "The item changed. Reload and try again.");
        }
        catch (CommandValidationException exception)
        {
            trace.Entries.Add("common:validation");
            return InputProblems.Problem(400, exception.Code ?? "invalid-request", "The request could not be completed.");
        }
        finally
        {
            trace.Entries.Add("common:exit");
        }
    }
}

public static class MembershipPolicy
{
    // Both specialized catches stay inside the application helper, before common validation mapping.
    public static async Task<Results<TSuccess, ProblemHttpResult>> RunInnerAsync<TSuccess>(Func<Task<TSuccess>> operation, InputTrace trace) where TSuccess : IResult
    {
        trace.Entries.Add("membership:enter");
        try
        {
            return await operation();
        }
        catch (ConfirmationRequiredException)
        {
            trace.Entries.Add("membership:confirmation");
            return InputProblems.Problem(409, "confirmation-required", "Confirm your identity before changing membership.");
        }
        catch (CommandValidationException exception) when (exception.Code == InputProblems.PendingCode)
        {
            trace.Entries.Add("membership:pending");
            return InputProblems.Problem(409, InputProblems.PendingCode, "An invitation is already saved. Refresh before continuing.");
        }
        finally
        {
            trace.Entries.Add("membership:exit");
        }
    }
}

public static class IntakeApplication
{
    // Explicit input decisions remain application outcomes, including their deliberate status overrides.
    public static async Task<Results<Accepted<IntakeReceipt>, ProblemHttpResult>> StartAsync(int workspaceId, InputRequest request, InputOperations operations, CancellationToken cancellationToken)
    {
        if (request.Outcome == "explicit400")
        {
            return InputProblems.Input("invalid-source", "source");
        }

        if (request.Outcome == "explicit413")
        {
            return InputProblems.Input("payload-size", "file", 413);
        }

        return TypedResults.Accepted("/intake/receipts/" + workspaceId, await operations.IntakeAsync(workspaceId, request, cancellationToken));
    }
}

public sealed class InputOperations(InputTrace trace, InputProbe probe)
{
    public Guid ScopeId => trace.ScopeId;

    public async Task<MembershipReceipt> InviteAsync(int workspaceId, InputRequest request, CancellationToken cancellationToken)
    {
        // Permission/resource work stays in this application callback, before the mutation.
        probe.Guard();
        trace.Entries.Add("guard:enter");
        try
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            if (workspaceId <= 0 || request.Outcome == "guard-denied")
            {
                throw new AccessDeniedException();
            }
        }
        finally
        {
            trace.Entries.Add("guard:exit");
        }

        await ExecuteAsync(request.Outcome, cancellationToken);
        return new MembershipReceipt(workspaceId);
    }

    public async Task<IntakeReceipt> IntakeAsync(int workspaceId, InputRequest request, CancellationToken cancellationToken)
    {
        await ExecuteAsync(request.Outcome, cancellationToken);
        return new IntakeReceipt(workspaceId);
    }

    public async Task<byte[]> ReportAsync(int workspaceId, string? outcome, Guid? receiptId, CancellationToken cancellationToken)
    {
        await ExecuteAsync(outcome, cancellationToken);
        return SyntheticReport.Content();
    }

    private async Task ExecuteAsync(string? outcome, CancellationToken cancellationToken)
    {
        probe.Operation();
        trace.Entries.Add("operation:enter");
        try
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            switch (outcome)
            {
                case "pending": throw new CommandValidationException(InputProblems.PendingCode);
                case "confirmation": throw new ConfirmationRequiredException();
                case "validation": throw new CommandValidationException("invalid-operation");
                case "input": throw new FieldInputException("invalid-source", "source");
                case "input-limit": throw new FieldInputException("payload-size", "file");
                case "forbidden": throw new AccessDeniedException();
                case "missing": throw new ResourceMissingException();
                case "conflict": throw new CommandConflictException();
                case "unknown": throw new UnexpectedInputException();
                case "cancel": throw new OperationCanceledException("Synthetic cancellation.", cancellationToken);
            }
        }
        finally
        {
            trace.Entries.Add("operation:exit");
        }
    }
}