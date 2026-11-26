using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.SharedResponsesExample;

public static class SharedResponseMetadata
{
    public const string Document = "v1";
    public const string Tag = "Shared responses";
    public const string ProblemJson = "application/problem+json";
}

public sealed record SharedReply(string Source, int Value);

public sealed record FilterError(string Code, string Message);

public sealed record EarlierError(string Earlier);

public sealed record MethodError(string Method);

public sealed record ConfiguredError(string Configured);

public sealed class SharedResponseProbe
{
    private readonly ConcurrentDictionary<string, int> calls = new();
    private int guardVisits;
    private int conventionalVisits;
    private int activations;
    private int disposals;
    public int Calls(string source) => calls.GetValueOrDefault(source);
    public int TotalCalls => calls.Values.Sum();
    public int GuardVisits => Volatile.Read(ref guardVisits);
    public int ConventionalVisits => Volatile.Read(ref conventionalVisits);
    public int Activations => Volatile.Read(ref activations);
    public int Disposals => Volatile.Read(ref disposals);
    public void Record(string source) => calls.AddOrUpdate(source, 1, (_, count) => count + 1);
    public void VisitGuard() => Interlocked.Increment(ref guardVisits);
    public void VisitConvention() => Interlocked.Increment(ref conventionalVisits);
    public void Activate() => Interlocked.Increment(ref activations);
    public void DisposeTarget() => Interlocked.Increment(ref disposals);
}

public static class SharedReplies
{
    public static Ok<SharedReply> Ok(SharedResponseProbe probe, string source, int value)
    {
        probe.Record(source);
        return TypedResults.Ok(new SharedReply(source, value));
    }
}