using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Timeouts;

namespace RonSijm.MediumAPI.RequestTimeoutsExample;

public static class TimeoutPolicies
{
    public const string Group = "group-timeout";
    public const string Class = "class-timeout";
    public const string Method = "method-timeout";
    public const string Default = "default-timeout";
    public const string Object = "object-timeout";
    public const string Missing = "missing-timeout";
    public const string CustomHeader = "X-Sample-Timeout-Policy";

    public static RequestTimeoutPolicy ObjectPolicy() => CreatePolicy(Object, TimeSpan.FromSeconds(30));

    public static RequestTimeoutPolicy CreatePolicy(string name, TimeSpan timeout) => new()
    {
        Timeout = timeout,
        TimeoutStatusCode = name switch { Class => StatusCodes.Status503ServiceUnavailable, Method => StatusCodes.Status502BadGateway, Object => StatusCodes.Status408RequestTimeout, _ => StatusCodes.Status504GatewayTimeout },
        WriteTimeoutResponse = context =>
        {
            context.RequestServices.GetRequiredService<TimeoutProbe>().RecordTimeoutResponse();
            context.Response.Headers[CustomHeader] = name;
            return Task.CompletedTask;
        }
    };
}

public sealed record TimeoutObservation(string Value, bool HasTimeoutFeature);

public sealed class TimeoutProbe
{
    private readonly ConcurrentDictionary<string, int> calls = new(StringComparer.Ordinal);
    private int totalCalls;
    private int cancellations;
    private int timeoutResponses;

    public int Calls => Volatile.Read(ref totalCalls);
    public int Cancellations => Volatile.Read(ref cancellations);
    public int TimeoutResponses => Volatile.Read(ref timeoutResponses);
    public int CallsFor(string value) => calls.TryGetValue(value, out var count) ? count : 0;

    public void Record(string value)
    {
        calls.AddOrUpdate(value, 1, (_, count) => count + 1);
        Interlocked.Increment(ref totalCalls);
    }

    public void RecordCancellation() => Interlocked.Increment(ref cancellations);
    public void RecordTimeoutResponse() => Interlocked.Increment(ref timeoutResponses);
}

internal static class SampleReplies
{
    internal static async Task<Ok<TimeoutObservation>> ReplyAsync(string value, HttpContext context, TimeoutProbe probe, bool wait)
    {
        probe.Record(value);
        var feature = context.Features.Get<IHttpRequestTimeoutFeature>();
        if (wait && feature is not null)
        {
            try { await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted); }
            catch (OperationCanceledException)
            {
                probe.RecordCancellation();
                throw;
            }
        }

        return TypedResults.Ok(new TimeoutObservation(value, feature is not null));
    }
}