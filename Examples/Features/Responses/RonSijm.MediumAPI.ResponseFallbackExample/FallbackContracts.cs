using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;

namespace RonSijm.MediumAPI.ResponseFallbackExample;

public sealed record FallbackReply(string Source, string Value);

public sealed record FirstFailure(string First);

public sealed record SecondFailure(string Second);

public sealed record FilterFailure(string Reason);

public sealed record NamespaceFailure(string Scope);

public sealed record ParentFailure(string Parent);

public sealed record ChildFailure(string Child);

public sealed record FinalFailure(string Final);

public sealed record LaterFailure(string Later);

public sealed record ProviderFailure(string Provider);

public sealed class FallbackProbe
{
    private readonly ConcurrentDictionary<string, int> calls = new(StringComparer.Ordinal);
    private int targetActivations;
    private int filterCalls;
    public int TotalCalls => calls.Values.Sum();
    public int TargetActivations => Volatile.Read(ref targetActivations);
    public int FilterCalls => Volatile.Read(ref filterCalls);

    public void ActivateTarget()
    {
        Interlocked.Increment(ref targetActivations);
    }

    public void Visit(string source)
    {
        calls.AddOrUpdate(source, 1, static (_, current) => current + 1);
    }

    public void VisitFilter()
    {
        Interlocked.Increment(ref filterCalls);
    }
}

public static class FallbackReplies
{
    public static Ok<FallbackReply> Ok(string source, FallbackProbe probe)
    {
        probe.Visit(source);
        return TypedResults.Ok(new FallbackReply(source, "available"));
    }

    public static Results<Ok<FallbackReply>, NotFound> Typed(string source, bool? absent, FallbackProbe probe)
    {
        probe.Visit(source);
        if (absent == true)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new FallbackReply(source, "available"));
    }

    public static Results<Ok<FallbackReply>, ProblemHttpResult> Problem(bool? problem, FallbackProbe probe)
    {
        probe.Visit("problem");
        if (problem == true)
        {
            return TypedResults.Problem(statusCode: 404, title: "The item is unavailable.", type: "urn:sample:missing-item", extensions: new Dictionary<string, object?> { ["code"] = "missing-item" });
        }

        return TypedResults.Ok(new FallbackReply("problem", "available"));
    }

    public static IResult Dynamic(bool? absent, FallbackProbe probe)
    {
        probe.Visit("dynamic");
        if (absent == true)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new FallbackReply("dynamic", "available"));
    }
}

public sealed class ProviderResult : IResult, IEndpointMetadataProvider
{
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new ProviderResponseMetadata());
    }

    public Task ExecuteAsync(HttpContext context)
    {
        return TypedResults.NoContent().ExecuteAsync(context);
    }
}

public sealed class ProviderResponseMetadata : IProducesResponseTypeMetadata
{
    public int StatusCode => StatusCodes.Status404NotFound;
    public Type? Type => typeof(ProviderFailure);
    public IEnumerable<string> ContentTypes => ["application/x-provider-error"];
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class UnrelatedFallbackAttribute : Attribute
{
    public UnrelatedFallbackAttribute()
    {
        throw new InvalidOperationException("Unrelated class attributes must not be constructed during selective declaration lookup.");
    }
}