using System.Collections.Concurrent;

namespace RonSijm.MediumAPI.EndpointCorsExample;

public sealed record CorsReceipt(string Handler);

public sealed class CorsProbe
{
    private readonly ConcurrentDictionary<string, int> calls = new(StringComparer.Ordinal);
    private int totalCalls;

    public int TotalCalls => Volatile.Read(ref totalCalls);
    public int CallsFor(string handler) => calls.TryGetValue(handler, out var count) ? count : 0;

    public CorsReceipt Record(string handler)
    {
        calls.AddOrUpdate(handler, 1, (_, count) => count + 1);
        Interlocked.Increment(ref totalCalls);
        return new CorsReceipt(handler);
    }
}