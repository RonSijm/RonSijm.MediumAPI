using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.RequestBodyLimitsExample;

public static class BodyLimits
{
    public const long Server = 2048;
    public const long Group = 1024;
    public const long Class = 512;
    public const long Method = 768;
    public const long Configured = 256;
    public const long Final = 128;
    public const long Reenabled = 128;
}

public sealed record BodyLimitObservation(string Source, int Bytes, long? EffectiveLimit, bool FeaturePresent, bool FeatureReadOnly, bool Chunked);

public sealed class BodyLimitProbe
{
    private readonly ConcurrentDictionary<string, int> entries = new();
    private readonly ConcurrentDictionary<string, int> completions = new();
    private int activations;
    private int disposals;
    public int Activations => Volatile.Read(ref activations);
    public int Disposals => Volatile.Read(ref disposals);
    public int Entered(string source) => entries.GetValueOrDefault(source);
    public int Completed(string source) => completions.GetValueOrDefault(source);
    public void RecordEntry(string source) => entries.AddOrUpdate(source, 1, (_, count) => count + 1);
    public void RecordCompletion(string source) => completions.AddOrUpdate(source, 1, (_, count) => count + 1);
    public void RecordActivation() => Interlocked.Increment(ref activations);
    public void RecordDisposal() => Interlocked.Increment(ref disposals);
}

public static class BodyLimitReader
{
    public static async Task<Ok<BodyLimitObservation>> ReadAsync(string source, HttpContext context, BodyLimitProbe probe)
    {
        probe.RecordEntry(source);
        await using var body = new MemoryStream();
        await context.Request.Body.CopyToAsync(body, context.RequestAborted);
        probe.RecordCompletion(source);
        var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        return TypedResults.Ok(new BodyLimitObservation(source, checked((int)body.Length), feature?.MaxRequestBodySize, feature is not null, feature?.IsReadOnly ?? false, context.Request.Headers.TransferEncoding.ToString().Equals("chunked", StringComparison.OrdinalIgnoreCase)));
    }
}