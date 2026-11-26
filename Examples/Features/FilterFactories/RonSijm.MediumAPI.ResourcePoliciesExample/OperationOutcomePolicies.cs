using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ResourcePoliciesExample;

public sealed record OperationOutcomeCommand(string Outcome, bool Update = false);

public sealed record CustomerOutcomeDto(long Id);

public sealed record GenerationOutcomeDto(long Id, string State);

public sealed record OperationOutcomeReceipt(long Id, bool Replayed);

public sealed class CompletedOperationException : Exception;

public sealed class ReplayConflictException(string code) : Exception("Internal synthetic replay detail")
{
    public string Code { get; } = code;
}

// Fixed synthetic outcomes demonstrate HTTP adapters, not an idempotency store or workflow engine.
public sealed class OperationOutcomeService(ResourceProbe probe, ResourceTrace trace)
{
    public async Task<OperationOutcomeReceipt> ExecuteAsync(string outcome, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        probe.Operations++;
        trace.Entries.Add("operation");
        Throw(outcome);
        return new OperationOutcomeReceipt(17, outcome == "replay");
    }

    public async Task<bool> OwnsAsync(string outcome, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        probe.OwnershipChecks++;
        trace.Entries.Add("ownership");
        Throw(outcome);
        return outcome != "ownership-missing";
    }

    private static void Throw(string outcome)
    {
        switch (outcome)
        {
            case "forbidden": throw new AccessDeniedException();
            case "missing": throw new MissingResourceException();
            case "validation": throw new InvalidResourceException();
            case "coded-validation": throw new InvalidResourceException("draft-not-ready");
            case "concurrency": throw new StaleResourceException();
            case "completed": throw new CompletedOperationException();
            case "conflict": throw new ReplayConflictException("idempotency-key-reused");
            case "pending": throw new ReplayConflictException("idempotency-request-in-progress");
            case "receipt": throw new ReplayConflictException("idempotency-one-time-receipt-issued");
            case "custom-conflict": throw new ReplayConflictException("custom-conflict");
            case "unknown": throw new UnexpectedResourceException();
            case "cancel": throw new OperationCanceledException("Synthetic cancellation");
        }
    }
}

public static class OperationOutcomeProblems
{
    public static ProblemHttpResult Create(int status, string code, string title)
    {
        return TypedResults.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code });
    }

    public static ProblemHttpResult Replay(ReplayConflictException exception)
    {
        return Create(409, exception.Code, exception.Code switch
        {
            "idempotency-request-in-progress" => "A request with this idempotency key is still being processed.",
            "idempotency-one-time-receipt-issued" => "The token was created. Check saved tokens; its one-time secret cannot be shown again.",
            _ => "This idempotency key was already used for a different request."
        });
    }

    public static void MarkReplay(HttpContext context, bool replayed)
    {
        if (replayed) { context.Response.Headers["Idempotent-Replayed"] = "true"; }
    }
}

// These specialized adapters deliberately add no response metadata. Keep existing outer policies.
// Exceptions for routine control flow are generally an anti-pattern; prefer explicit expected outcomes.
public sealed class ReplayConflictOutcomeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (ReplayConflictException exception) { return OperationOutcomeProblems.Replay(exception); }
    }
}

public sealed class CustomerCreateOutcomeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (AccessDeniedException) { return TypedResults.Forbid(); }
        catch (ReplayConflictException exception) { return OperationOutcomeProblems.Replay(exception); }
        catch (InvalidResourceException) { return OperationOutcomeProblems.Create(400, "invalid-request", "The request could not be completed."); }
    }
}

public sealed class CompletedOperationOutcomeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (CompletedOperationException) { return OperationOutcomeProblems.Create(409, "operation-already-completed", "The operation cannot be cancelled."); }
    }
}

public sealed class StaleCatalogOutcomeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (StaleResourceException) { return OperationOutcomeProblems.Create(409, "stale-catalog-entry", "Reload the current entry before saving."); }
    }
}

public sealed class IntakeValidationOutcomeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (InvalidResourceException exception) { return OperationOutcomeProblems.Create(400, exception.Code ?? "invalid-request", "The request could not be completed."); }
    }
}

[EndpointResponse(401)]
[EndpointResponse(403)]
[EndpointResponse(404)]
public sealed class ExistingReadOutcomeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (AccessDeniedException) { return TypedResults.Forbid(); }
        catch (MissingResourceException) { return TypedResults.NotFound(); }
    }
}

[EndpointResponse(400)]
[EndpointResponse(401)]
[EndpointResponse(403)]
[EndpointResponse(404)]
public sealed class ExistingRequestOutcomeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (AccessDeniedException) { return TypedResults.Forbid(); }
        catch (MissingResourceException) { return TypedResults.NotFound(); }
        catch (InvalidResourceException) { return OperationOutcomeProblems.Create(400, "invalid-request", "The request could not be completed."); }
    }
}

public sealed class ExistingCommandOutcomeFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try { return await next(context); }
        catch (AccessDeniedException) { return TypedResults.Forbid(); }
        catch (MissingResourceException) { return TypedResults.NotFound(); }
        catch (ReplayConflictException exception) { return OperationOutcomeProblems.Replay(exception); }
        catch (StaleResourceException) { return OperationOutcomeProblems.Create(409, "concurrency-conflict", "The item was changed by someone else. Reload and try again."); }
        catch (InvalidResourceException exception) { return OperationOutcomeProblems.Create(400, exception.Code ?? "invalid-request", "The request could not be completed."); }
        catch (CompletedOperationException) { return OperationOutcomeProblems.Create(400, "invalid-request", "The request could not be completed."); }
    }
}