using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.AcceptsExample;

public sealed record PublishRequest(string Name, int Quantity);

public sealed record UploadDescription(string Title, IFormFile File);

public sealed record BodyReply(string Source, string? Value, int Size);

public sealed record UploadReply(string Title, string FileName, long Length);

public sealed class RequestProbe
{
    private readonly ConcurrentDictionary<string, int> calls = new(StringComparer.Ordinal);
    private int filterCalls;
    public int TotalCalls => calls.Values.Sum();
    public int FilterCalls => Volatile.Read(ref filterCalls);
    public int Calls(string source) => calls.GetValueOrDefault(source);
    public void Visit(string source) => calls.AddOrUpdate(source, 1, static (_, value) => value + 1);
    public void VisitFilter() => Interlocked.Increment(ref filterCalls);
}

public static class RequestBodies
{
    public static Ok<BodyReply> Json(string source, PublishRequest? request, RequestProbe probe)
    {
        probe.Visit(source);
        return TypedResults.Ok(new BodyReply(source, request?.Name, request?.Quantity ?? 0));
    }

    public static async Task<Ok<BodyReply>> Raw(string source, HttpContext context, RequestProbe probe, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        probe.Visit(source);
        return TypedResults.Ok(new BodyReply(source, text, text.Length));
    }

    public static async Task<Ok<UploadReply>> Upload(HttpContext context, RequestProbe probe, CancellationToken cancellationToken)
    {
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var file = form.Files.Single();
        probe.Visit("upload");
        return TypedResults.Ok(new UploadReply(form["title"].ToString(), file.FileName, file.Length));
    }
}