using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample;

public static class RateLimitPolicies
{
    public const string Class = "class-policy";
    public const string Method = "method-policy";
    public const string Fluent = "fluent-policy";
    public const string Group = "group-policy";
    public const string Missing = "missing-policy";
    public const string RejectionHeader = "X-Sample-Rejected";
    public const string RejectionValue = "rate-limit";

    internal static void AddFixedWindow(RateLimiterOptions options, string name, int permits)
    {
        options.AddFixedWindowLimiter(name, limiter =>
        {
            limiter.PermitLimit = permits;
            limiter.Window = TimeSpan.FromDays(1);
            limiter.AutoReplenishment = false;
            limiter.QueueLimit = 0;
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });
    }
}

public sealed record LimitResponse(string Value);

public sealed class RateLimitProbe
{
    private readonly ConcurrentDictionary<string, int> calls = new(StringComparer.Ordinal);
    private int totalCalls;
    private int rejections;

    public int TotalCalls => Volatile.Read(ref totalCalls);
    public int Rejections => Volatile.Read(ref rejections);

    public int CallsFor(string value) => calls.TryGetValue(value, out var count) ? count : 0;

    public void Record(string value)
    {
        calls.AddOrUpdate(value, 1, (_, count) => count + 1);
        Interlocked.Increment(ref totalCalls);
    }

    public void RecordRejection() => Interlocked.Increment(ref rejections);
}

internal static class SampleReplies
{
    internal static Ok<LimitResponse> Reply(string value, RateLimitProbe probe)
    {
        probe.Record(value);
        return TypedResults.Ok(new LimitResponse(value));
    }
}