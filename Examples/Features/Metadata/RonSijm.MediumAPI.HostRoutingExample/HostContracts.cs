using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.HostRoutingExample;

public static class SampleHosts
{
    public const string Group = "group.example.test";
    public const string Api = "api.example.test";
    public const string Alternate = "alternate.example.test";
    public const string Method = "method.example.test";
    public const string Configured = "configured.example.test";
    public const string Wildcard = "*.example.test";
    public const string Port = "port.example.test:8443";
    public const string SecurePort = "secure.example.test:443";
    public const string HttpPort = "web.example.test:80";
    public const string Child = "child.example.test";
}

public sealed record HostResponse(string Value);

public sealed class HostProbe
{
    private readonly ConcurrentDictionary<string, int> calls = new(StringComparer.Ordinal);
    private int totalCalls;
    public int Calls => Volatile.Read(ref totalCalls);
    public int CallsFor(string value) => calls.TryGetValue(value, out var count) ? count : 0;

    public HostResponse Record(string value)
    {
        calls.AddOrUpdate(value, 1, (_, count) => count + 1);
        Interlocked.Increment(ref totalCalls);
        return new HostResponse(value);
    }
}

internal static class SampleReplies
{
    internal static Ok<HostResponse> Reply(HostProbe probe, string value) => TypedResults.Ok(probe.Record(value));
}