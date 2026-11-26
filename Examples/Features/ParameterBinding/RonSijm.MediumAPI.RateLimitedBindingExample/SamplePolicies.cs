using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.RateLimitedBindingExample;

public static class SamplePolicies
{
    public const string Named = "customer-default";
    public const string RejectionHeader = "X-Sample-Rejected";
    public const string HostRejection = "host";
    public const string TypedRejection = "typed";

    public static string CreateNamedPolicyName(IServiceProvider services)
    {
        services.GetRequiredService<BindingProbe>().RecordNamedFactory();
        return Named;
    }

    public static IRateLimiterPolicy<string> CreateTypedPolicy(IServiceProvider services)
    {
        var probe = services.GetRequiredService<BindingProbe>();
        probe.RecordTypedFactory();
        return new CustomerRateLimitPolicy(probe);
    }

    public static ValueTask Reject(OnRejectedContext context, string reason)
    {
        context.HttpContext.RequestServices.GetRequiredService<BindingProbe>().RecordRejection();
        context.HttpContext.Response.Headers[RejectionHeader] = reason;
        return ValueTask.CompletedTask;
    }
}

public sealed class CustomerRateLimitPolicy(BindingProbe probe) : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => (context, cancellationToken) => SamplePolicies.Reject(context, SamplePolicies.TypedRejection);

    public RateLimitPartition<string> GetPartition(HttpContext context)
    {
        probe.RecordPartition();
        var customer = context.Request.RouteValues["customerId"]?.ToString() ?? "missing-customer";
        return RateLimitPartition.GetFixedWindowLimiter(customer, partition => new FixedWindowRateLimiterOptions { PermitLimit = 2, Window = TimeSpan.FromDays(1), AutoReplenishment = false, QueueLimit = 0 });
    }
}

public sealed record BindingResponse(string Kind, long CustomerId, int? ItemId);

public sealed class BindingProbe
{
    private int namedFactories;
    private int typedFactories;
    private int partitions;
    private int dispatches;
    private int rejections;

    public int NamedFactories => Volatile.Read(ref namedFactories);
    public int TypedFactories => Volatile.Read(ref typedFactories);
    public int Partitions => Volatile.Read(ref partitions);
    public int Dispatches => Volatile.Read(ref dispatches);
    public int Rejections => Volatile.Read(ref rejections);

    public void RecordNamedFactory() => Interlocked.Increment(ref namedFactories);
    public void RecordTypedFactory() => Interlocked.Increment(ref typedFactories);
    public void RecordPartition() => Interlocked.Increment(ref partitions);
    public void RecordDispatch() => Interlocked.Increment(ref dispatches);
    public void RecordRejection() => Interlocked.Increment(ref rejections);
}