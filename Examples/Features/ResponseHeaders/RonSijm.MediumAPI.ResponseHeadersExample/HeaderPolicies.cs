using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.ResponseHeadersExample;

public sealed record HeaderItem(int Id, string Name);

public sealed record HeaderConfigurationMarker;

[ResponseHeader("X-Filter-Base", "ignored")]
public abstract class HeaderFilterBase { }

[ResponseHeader("X-Filter", "attribute")]
[ResponseHeader("X-Filter-Outcome", "blocked", ResultType = typeof(Conflict<>))]
public sealed class HeaderPolicyFilter : HeaderFilterBase, IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.WithResponseHeader("X-Configured", "configured");
        endpoint.WithMetadata(new HeaderConfigurationMarker());
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.Request.Query.ContainsKey("block"))
        {
            return TypedResults.Conflict("filter blocked");
        }

        try { return await next(context); }
        catch (InvalidOperationException) when (context.HttpContext.Request.Query.ContainsKey("recover")) { return TypedResults.Conflict("translated synthetic failure"); }
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class UnrelatedHeaderExampleAttribute : Attribute
{
    public UnrelatedHeaderExampleAttribute() => throw new InvalidOperationException("Unrelated class attributes must not be instantiated for header policies.");
}