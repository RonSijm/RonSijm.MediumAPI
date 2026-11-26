using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ResourcePoliciesExample;

public sealed record GuardProfileDto(string DisplayName);

public sealed record GuardProfileRequest(GuardProfileDto? Profile, string Outcome = "ok");

public sealed record GuardSupportDto(long Id, string Title);

// Fixed synthetic operations show adapter boundaries, not authorization, paging or profile services.
public sealed class NamedArgumentOperations(ResourceProbe probe, ResourceTrace trace)
{
    public async Task<GuardProfileDto> UpdateProfileAsync(GuardProfileDto profile, string outcome, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        probe.Operations++;
        trace.Entries.Add("update-profile");
        Throw(outcome);
        return profile;
    }

    public async Task<IReadOnlyList<GuardSupportDto>> ListSupportAsync(long? customerId, long? projectId, string outcome, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        probe.Reads++;
        trace.Entries.Add(customerId.HasValue ? "customer-support" : "all-support");
        Throw(outcome);
        return [new GuardSupportDto(7, projectId.HasValue ? "Project request" : "Customer request")];
    }

    public async Task<int> ReadOverviewAsync(string outcome, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        probe.Reads++;
        trace.Entries.Add("overview");
        if (outcome.StartsWith("first-", StringComparison.Ordinal)) { Throw(outcome[6..]); }

        return 17;
    }

    public async Task<int> ReadSettingsAsync(string outcome, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        probe.Reads++;
        trace.Entries.Add("settings");
        if (outcome.StartsWith("later-", StringComparison.Ordinal)) { Throw(outcome[6..]); }

        return 23;
    }

    private static void Throw(string outcome)
    {
        switch (outcome)
        {
            case "forbidden": throw new AccessDeniedException();
            case "missing": throw new MissingResourceException();
            case "validation": throw new InvalidResourceException();
            case "unknown": throw new UnexpectedResourceException();
            case "cancel": throw new OperationCanceledException("Synthetic cancellation");
        }
    }
}

// Factories have no response declarations; existing handlers and outer policies retain metadata.
public sealed class ProfileArgumentGuardFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        if (!context.TryGetArgumentIndex<GuardProfileRequest>(out var requestIndex))
        {
            throw new InvalidOperationException("ProfileArgumentGuardFactory requires exactly one profile request.");
        }

        return invocation =>
        {
            if (invocation.GetArgument<GuardProfileRequest>(requestIndex).Profile is null)
            {
                Results<BadRequest, Ok<GuardProfileDto>> result = TypedResults.BadRequest();
                return ValueTask.FromResult<object?>(result);
            }

            return next(invocation);
        };
    }
}

public sealed class SupportArgumentGuardFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        if (!context.TryGetArgumentIndex<long?>("customerId", out var customerIndex) || !context.TryGetArgumentIndex<long?>("projectId", out var projectIndex))
        {
            throw new InvalidOperationException("SupportArgumentGuardFactory requires named nullable customer and project parameters.");
        }

        return invocation =>
        {
            var customerId = invocation.GetArgument<long?>(customerIndex);
            var projectId = invocation.GetArgument<long?>(projectIndex);
            if (!customerId.HasValue && projectId.HasValue)
            {
                return ValueTask.FromResult<object?>(Results.BadRequest(new { error = "projectId requires customerId." }));
            }

            return next(invocation);
        };
    }
}

public sealed class OverviewArgumentGuardFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        if (!context.TryGetArgumentIndex<int?>("runningPage", out var runningIndex) || !context.TryGetArgumentIndex<int?>("waitingPage", out var waitingIndex) || !context.TryGetArgumentIndex<int?>("finishedPage", out var finishedIndex) || !context.TryGetArgumentIndex<int?>("projectPage", out var projectIndex))
        {
            throw new InvalidOperationException("OverviewArgumentGuardFactory requires its four named nullable page parameters.");
        }

        return invocation =>
        {
            foreach (var page in new[] { invocation.GetArgument<int?>(runningIndex), invocation.GetArgument<int?>(waitingIndex), invocation.GetArgument<int?>(finishedIndex), invocation.GetArgument<int?>(projectIndex) })
            {
                if (page is <= 0 or > 100000) { throw new InvalidResourceException(); }
            }

            return next(invocation);
        };
    }
}