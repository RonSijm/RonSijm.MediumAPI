using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.BindingMetadataExample;

public static class HeaderBindingMetadata
{
    public const string HeaderName = "Idempotency-Key";
    public const string Tag = "Named header inputs";
    public const string Location = "/header-commands/receipts/1";
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class CommandKeyAttribute : FromHeaderAttribute
{
    public CommandKeyAttribute()
    {
        Name = HeaderBindingMetadata.HeaderName;
    }
}

public sealed record HeaderCommand(string Name);

public sealed record HeaderReceipt(string Name, string? Key, string Kind);

public sealed record HeaderFactoryVisit(int Index, int ArgumentCount, string? Key);

public sealed class HeaderBindingProbe
{
    private readonly ConcurrentQueue<HeaderFactoryVisit> factoryVisits = new();
    private readonly ConcurrentQueue<HeaderReceipt> serviceCalls = new();
    private int entries;
    private int formReads;
    public IReadOnlyList<HeaderFactoryVisit> FactoryVisits => factoryVisits.ToArray();
    public IReadOnlyList<HeaderReceipt> ServiceCalls => serviceCalls.ToArray();
    public int Entries => Volatile.Read(ref entries);
    public int FormReads => Volatile.Read(ref formReads);

    public void RecordFactory(int index, int count, string? key)
    {
        factoryVisits.Enqueue(new HeaderFactoryVisit(index, count, key));
    }

    public void RecordEntry(bool readForm = false)
    {
        if (readForm) { Interlocked.Increment(ref formReads); }
        else { Interlocked.Increment(ref entries); }
    }

    public void RecordService(HeaderReceipt receipt)
    {
        serviceCalls.Enqueue(receipt);
    }
}

public sealed class HeaderCommandService(HeaderBindingProbe probe)
{
    public Task<HeaderReceipt> CreateAsync(string name, string? key, string kind, CancellationToken token)
    {
        var receipt = new HeaderReceipt(name, key, kind);
        probe.RecordService(receipt);
        return Task.FromResult(receipt);
    }
}

[EndpointResponse(400)]
public sealed class HeaderBindingFactory : IMediumApiEndpointFilterFactory
{
    private HeaderBindingFactory()
    {
        throw new InvalidOperationException("Header binding factories are static declarations.");
    }

    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        if (!context.TryGetArgumentIndex<string>("idempotencyKey", out var keyIndex)) { throw new InvalidOperationException("One named string header parameter is required."); }

        var hasBody = context.TryGetArgumentIndex<HeaderCommand>(out var bodyIndex);
        return invocation =>
        {
            var key = invocation.GetArgument<string?>(keyIndex);
            invocation.HttpContext.RequestServices.GetRequiredService<HeaderBindingProbe>().RecordFactory(keyIndex, invocation.Arguments.Count, key);
            if (hasBody && string.IsNullOrWhiteSpace(invocation.GetArgument<HeaderCommand?>(bodyIndex)?.Name)) { return ValueTask.FromResult<object?>(TypedResults.BadRequest()); }

            return next(invocation);
        };
    }
}

public static class NamedHeaderScenario
{
    public static void Map(WebApplication app)
    {
        app.TryMapMediumApiNamespace<NamedHeaders.NamedHeadersGroupDefinition>();
    }
}