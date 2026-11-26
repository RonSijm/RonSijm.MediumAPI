using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointDocumentationExample;

public sealed record AliasConventionStage(string Value);

public sealed record AliasConventionReply(string Kind, int? Page, string[] Stages);

public sealed class AliasConventionProbe
{
    private readonly ConcurrentQueue<string> events = new();
    private readonly ConcurrentQueue<string> materializations = new();
    private int calls;
    public IReadOnlyList<string> Events => events.ToArray();
    public IReadOnlyList<string> Materializations => materializations.ToArray();
    public int Calls => Volatile.Read(ref calls);

    public void RecordEvent(string value)
    {
        events.Enqueue(value);
    }

    public void RecordMaterialization(string value)
    {
        materializations.Enqueue(value);
    }

    public void RecordHandler(string kind)
    {
        Interlocked.Increment(ref calls);
        events.Enqueue("handler:" + kind);
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class UnrelatedAliasDecorationAttribute : Attribute
{
    public UnrelatedAliasDecorationAttribute()
    {
        throw new InvalidOperationException("Decorations on alias or convention types must not be imported.");
    }
}

public static class AliasConventionCallbacks
{
    public static void Add(IEndpointConventionBuilder builder, string stage, string? header = null, bool final = false)
    {
        builder.Add(endpoint =>
        {
            endpoint.Metadata.Add(new AliasConventionStage(stage));
            endpoint.ApplicationServices.GetRequiredService<AliasConventionProbe>().RecordMaterialization(stage);
        });
        if (header is not null) { builder.WithResponseHeader(header, stage); }

        if (final)
        {
            builder.Finally(endpoint =>
            {
                endpoint.Metadata.Add(new AliasConventionStage(stage + "-finally"));
                endpoint.ApplicationServices.GetRequiredService<AliasConventionProbe>().RecordMaterialization(stage + "-finally");
            });
        }
    }
}

public sealed class AliasScopeConvention : IMediumApiEndpointConvention
{
    private AliasScopeConvention()
    {
        throw new InvalidOperationException("A static scope convention is never constructed.");
    }

    public static void Configure(IEndpointConventionBuilder builder)
    {
        AliasConventionCallbacks.Add(builder, "namespace", "X-Alias-Scope", final: true);
    }
}

[Tags("Ignored convention type")]
[EndpointResponse(418)]
[ResponseHeader("X-Ignored-Convention-Type", "ignored")]
[UnrelatedAliasDecoration]
public sealed class AliasClassConvention : IMediumApiEndpointConvention
{
    private AliasClassConvention()
    {
        throw new InvalidOperationException("A static class convention is never constructed.");
    }

    public static void Configure(IEndpointConventionBuilder builder)
    {
        AliasConventionCallbacks.Add(builder, "class", "X-Alias-Class");
    }
}

public sealed class AliasMethodConvention : IMediumApiEndpointConvention
{
    private AliasMethodConvention()
    {
        throw new InvalidOperationException("A static method convention is never constructed.");
    }

    public static void Configure(IEndpointConventionBuilder builder)
    {
        AliasConventionCallbacks.Add(builder, "method", "X-Alias-Method");
    }
}

public readonly struct AliasStructConvention : IMediumApiEndpointConvention
{
    static void IMediumApiEndpointConvention.Configure(IEndpointConventionBuilder builder)
    {
        AliasConventionCallbacks.Add(builder, "explicit-struct");
    }
}

public sealed class AliasExplicitClassConvention : IMediumApiEndpointConvention
{
    private AliasExplicitClassConvention()
    {
        throw new InvalidOperationException("An explicit static class convention is never constructed.");
    }

    static void IMediumApiEndpointConvention.Configure(IEndpointConventionBuilder builder)
    {
        AliasConventionCallbacks.Add(builder, "explicit-class");
    }
}

public sealed class AliasThrowConvention : IMediumApiEndpointConvention
{
    public static InvalidOperationException Failure { get; } = new("Synthetic alias convention failure.");

    private AliasThrowConvention()
    {
        throw new InvalidOperationException("A throwing static convention is never constructed.");
    }

    public static void Configure(IEndpointConventionBuilder builder)
    {
        throw Failure;
    }
}

public sealed class NamedScopeAttribute : EndpointConventionAttribute<AliasScopeConvention> { }

[Tags("Ignored alias type")]
[EndpointResponse(419)]
[ResponseHeader("X-Ignored-Alias-Type", "ignored")]
[UnrelatedAliasDecoration]
public sealed class NamedClassAttribute : EndpointConventionAttribute<AliasClassConvention> { }

public sealed class NamedMethodAttribute : EndpointConventionAttribute<AliasMethodConvention> { }

public sealed class NamedStructAttribute : EndpointConventionAttribute<AliasStructConvention> { }

public sealed class NamedExplicitClassAttribute : EndpointConventionAttribute<AliasExplicitClassConvention> { }

public sealed class NamedFailureAttribute : EndpointConventionAttribute<AliasThrowConvention> { }

public sealed class AliasConventionGuard : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var probe = context.HttpContext.RequestServices.GetRequiredService<AliasConventionProbe>();
        probe.RecordEvent("guard-enter");
        if (context.HttpContext.Request.Headers.ContainsKey("X-Reject"))
        {
            probe.RecordEvent("guard-reject");
            return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
        }

        var result = await next(context);
        probe.RecordEvent("guard-exit");
        return result;
    }
}

public static class AliasConventionBodies
{
    public static Ok<AliasConventionReply> Create(string kind, int? page, HttpContext context, AliasConventionProbe probe)
    {
        probe.RecordHandler(kind);
        return TypedResults.Ok(new AliasConventionReply(kind, page, context.GetEndpoint()!.Metadata.GetOrderedMetadata<AliasConventionStage>().Select(stage => stage.Value).ToArray()));
    }
}

public static class NamedConventionSample
{
    public static void Map(WebApplication app)
    {
        app.TryMapMediumApiNamespace<NamedAliases.NamedConventionGroupDefinition>(ConfigureMount);
    }

    public static void ConfigureMount(RouteGroupBuilder group)
    {
        AliasConventionCallbacks.Add(group, "host", final: true);
    }
}