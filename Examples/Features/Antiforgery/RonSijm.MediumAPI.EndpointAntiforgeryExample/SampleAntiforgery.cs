using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample;

public static class SampleAntiforgery
{
    public const string HeaderName = "X-Sample-Antiforgery";
    public const string CookieName = "MediumApi.Sample.EndpointAntiforgery";
    public const string FormFieldName = "__RequestVerificationToken";
}

public sealed record AntiforgeryTokenResponse(string RequestToken, string? HeaderName, string FormFieldName);

public sealed record JsonSubmission(string Message);

public sealed record FormReceipt(string Message, string Handler, bool? AntiforgeryIsValid = null);

public sealed class FormProbe
{
    private readonly ConcurrentDictionary<string, int> calls = new(StringComparer.Ordinal);
    private int totalCalls;

    public int TotalCalls => Volatile.Read(ref totalCalls);
    public int CallsFor(string handler) => calls.TryGetValue(handler, out var count) ? count : 0;

    public void Record(string handler)
    {
        calls.AddOrUpdate(handler, 1, (_, count) => count + 1);
        Interlocked.Increment(ref totalCalls);
    }
}

internal static class SampleReplies
{
    internal static Ok<FormReceipt> Reply(string message, string handler, FormProbe probe, bool? antiforgeryIsValid = null)
    {
        probe.Record(handler);
        return TypedResults.Ok(new FormReceipt(message, handler, antiforgeryIsValid));
    }
}