namespace RonSijm.MediumAPI.ResourcePoliciesExample;

public sealed record ResourceDto(int Id, string Name);

public sealed record QueueReceipt(int Id, bool Existing);

public sealed record ResourceCommand(string Outcome);

public sealed record ReportExport(byte[] Content, string MediaType, string FileName);

public sealed class ResourceProbe
{
    public int OwnershipChecks { get; set; }
    public int Operations { get; set; }
    public int Reads { get; set; }
}

public sealed class ResourceTrace
{
    public List<string> Entries { get; } = [];
}

public sealed class AccessDeniedException : Exception;

public sealed class MissingResourceException : Exception;

public sealed class StaleResourceException : Exception;

public sealed class RemoteResourceMissingException : Exception;

public sealed class InvalidResourceException(string? code = null) : Exception("Internal synthetic detail")
{
    public string? Code { get; } = code;
}

public sealed class UnexpectedResourceException : Exception;

// Synthetic operations demonstrate transport boundaries, not a repository or authorization subsystem.
public sealed class ResourceOperations(ResourceProbe probe, ResourceTrace trace)
{
    public async Task<bool> BelongsAsync(string outcome, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        probe.OwnershipChecks++;
        trace.Entries.Add("ownership");
        return outcome != "ownership-mismatch";
    }

    public async Task<ResourceDto> CommandAsync(int id, string outcome, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        probe.Operations++;
        trace.Entries.Add("command");
        Throw(outcome);
        return new ResourceDto(id, "Synthetic resource");
    }

    public async Task<ResourceDto> ReadAsync(int id, string outcome, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        probe.Reads++;
        trace.Entries.Add("authorize-read");
        Throw(outcome);
        return new ResourceDto(id, "Synthetic resource");
    }

    public async Task<ReportExport> ExportAsync(string format, string outcome, CancellationToken cancellationToken)
    {
        await ReadAsync(7, outcome, cancellationToken);
        return format == "html" ? new ReportExport([60, 112, 62, 79, 75, 60, 47, 112, 62], "text/html", "report.html") : new ReportExport([37, 80, 68, 70, 45, 83, 65, 77, 80, 76, 69], "application/pdf", "report.pdf");
    }

    private static void Throw(string outcome)
    {
        switch (outcome)
        {
            case "forbidden": throw new AccessDeniedException();
            case "missing": throw new MissingResourceException();
            case "concurrency": throw new StaleResourceException();
            case "provider": throw new RemoteResourceMissingException();
            case "validation": throw new InvalidResourceException();
            case "coded-validation": throw new InvalidResourceException("private-code");
            case "unknown": throw new UnexpectedResourceException();
            case "cancel": throw new OperationCanceledException("Synthetic cancellation");
        }
    }
}