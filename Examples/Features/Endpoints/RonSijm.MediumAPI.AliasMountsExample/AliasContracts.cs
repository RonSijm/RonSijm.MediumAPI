namespace RonSijm.MediumAPI.AliasMountsExample;

public sealed record AliasReply(int Id, string? Note, string Path);

public sealed record AliasFinalMetadata(string Scope);

public sealed class AggregateConfigurationMetadata
{
    public int Touches { get; set; }
}

public sealed class PerMappingConfigurationMetadata { }

public sealed class AliasProbe
{
    private readonly TaskCompletionSource<bool> firstDisposal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int activations;
    private int calls;
    private int disposals;
    public int Activations => Volatile.Read(ref activations);
    public int Calls => Volatile.Read(ref calls);
    public int Disposals => Volatile.Read(ref disposals);
    public Task FirstDisposal => firstDisposal.Task;

    public void Activate()
    {
        Interlocked.Increment(ref activations);
    }

    public void Call()
    {
        Interlocked.Increment(ref calls);
    }

    public void DisposeTarget()
    {
        Interlocked.Increment(ref disposals);
        firstDisposal.TrySetResult(true);
    }
}

[EndpointResponse(StatusCodes.Status409Conflict)]
public sealed class AggregateAliasFilter : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.WithMetadata(new AggregateConfigurationMetadata());
    }

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.Response.Headers["X-Filter"] = "aggregate";
        if (context.HttpContext.Request.Query.ContainsKey("stop"))
        {
            return ValueTask.FromResult<object?>(TypedResults.StatusCode(StatusCodes.Status409Conflict));
        }

        return next(context);
    }
}