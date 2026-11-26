using System.Collections.Concurrent;

namespace RonSijm.MediumAPI.EndpointAuthorizationExample;

public sealed record AuthorizationReceipt(string Handler, bool IsAuthenticated);

public sealed class AuthorizationProbe
{
    private readonly ConcurrentDictionary<string, int> calls = new(StringComparer.Ordinal);
    private int totalCalls;
    public int TotalCalls => Volatile.Read(ref totalCalls);
    public int CallsFor(string handler) => calls.GetValueOrDefault(handler);

    public AuthorizationReceipt Record(string handler, bool authenticated)
    {
        calls.AddOrUpdate(handler, 1, (_, count) => count + 1);
        Interlocked.Increment(ref totalCalls);
        return new AuthorizationReceipt(handler, authenticated);
    }
}