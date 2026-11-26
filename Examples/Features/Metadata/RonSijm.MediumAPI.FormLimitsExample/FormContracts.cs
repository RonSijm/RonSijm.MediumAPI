using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.FormLimitsExample;

public static class FormLimits
{
    public const int HostCount = 1;
    public const int HostKey = 5;
    public const int HostValue = 5;
    public const long HostMultipart = 16;
    public const int GroupCount = 3;
    public const int GroupKey = 12;
    public const int GroupValue = 16;
    public const long GroupMultipart = 32;
    public const int ClassCount = 2;
    public const int ClassKey = 8;
    public const int ClassValue = 8;
    public const int MethodCount = 4;
    public const int MethodKey = 10;
    public const int MethodValue = 12;
    public const int ConfiguredValue = 4;
    public const int FinalValue = 2;
}

public sealed record FormReceipt(string Source, int Fields, int Files, long FileBytes, string[] Keys, string[] Values, bool ReusedForm);

public sealed class FormProbe
{
    private readonly ConcurrentDictionary<string, int> calls = new();
    private int activations;
    private int disposals;
    public int Activations => Volatile.Read(ref activations);
    public int Disposals => Volatile.Read(ref disposals);
    public int Calls(string source) => calls.GetValueOrDefault(source);
    public void Record(string source) => calls.AddOrUpdate(source, 1, (_, count) => count + 1);
    public void Activate() => Interlocked.Increment(ref activations);
    public void DisposeTarget() => Interlocked.Increment(ref disposals);
}

public static class FormReplies
{
    public static async Task<Ok<FormReceipt>> Fields(string source, IFormCollection form, HttpContext context, FormProbe probe)
    {
        probe.Record(source);
        var repeated = await context.Request.ReadFormAsync(context.RequestAborted);
        var keys = form.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray();
        return TypedResults.Ok(new FormReceipt(source, form.Count, form.Files.Count, form.Files.Sum(file => file.Length), keys, keys.Select(key => form[key].ToString()).ToArray(), ReferenceEquals(form, repeated)));
    }

    public static async Task<Ok<FormReceipt>> File(string source, IFormFile file, HttpContext context, FormProbe probe)
    {
        probe.Record(source);
        await using var content = file.OpenReadStream();
        await using var copied = new MemoryStream();
        await content.CopyToAsync(copied, context.RequestAborted);
        var repeated = await context.Request.ReadFormAsync(context.RequestAborted);
        return TypedResults.Ok(new FormReceipt(source, repeated.Count, repeated.Files.Count, copied.Length, repeated.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray(), [], ReferenceEquals(file, repeated.Files.GetFile("file"))));
    }
}