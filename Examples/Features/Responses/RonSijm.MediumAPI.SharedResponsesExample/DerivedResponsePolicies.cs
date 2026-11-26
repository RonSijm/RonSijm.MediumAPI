using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.SharedResponsesExample;

public sealed class ProblemResponseAttribute : EndpointResponseAttribute
{
    public ProblemResponseAttribute(int status) : base(status, typeof(ProblemDetails), SharedResponseMetadata.ProblemJson) { }
}

public sealed class DerivedResponseAttribute : EndpointResponseAttribute
{
    public DerivedResponseAttribute(int status, Type? responseType = null, string? contentType = null, params string[] additionalContentTypes) : base(status, responseType, contentType, additionalContentTypes) { }
}

// Dual opt-in still has one response owner; the generic custom-metadata pass skips it.
public sealed class MarkedProblemResponseAttribute : EndpointResponseAttribute, IEndpointMetadataAttribute
{
    private static readonly ConcurrentDictionary<int, int> constructions = new();

    public MarkedProblemResponseAttribute(int status) : base(status, typeof(ProblemDetails), SharedResponseMetadata.ProblemJson)
    {
        constructions.AddOrUpdate(status, 1, (_, count) => count + 1);
    }

    public static int Constructions(int status)
    {
        return constructions.GetValueOrDefault(status);
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ResponsePhaseAttribute : Attribute, IEndpointMetadataAttribute
{
    public ResponsePhaseAttribute(string phase)
    {
        Phase = phase;
    }

    public string Phase { get; }
}

public sealed record ResponseConfigurationPhase(string Phase);

[ProblemResponse(400)]
[MarkedProblemResponse(409)]
[ResponsePhase("custom-filter")]
[ProblemResponse(422)]
[MarkedProblemResponse(429)]
public sealed class DerivedProblemFilter : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        builder.WithMetadata(new ResponseConfigurationPhase("configure-filter"));
    }

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.RequestServices.GetRequiredService<SharedResponseProbe>().VisitGuard();
        return context.GetArgument<int>(0) < 0 ? ValueTask.FromResult<object?>(TypedResults.Problem(statusCode: 400, title: "Use a nonnegative value.", type: "urn:sample:derived-negative")) : next(context);
    }
}

[ProblemResponse(401)]
[MarkedProblemResponse(403)]
[ResponsePhase("custom-factory")]
[ProblemResponse(406)]
[MarkedProblemResponse(410)]
public sealed class DerivedProblemFactory : IMediumApiEndpointFilterFactory
{
    private DerivedProblemFactory()
    {
        throw new InvalidOperationException("A static factory is not instantiated.");
    }

    public static void Configure(IEndpointConventionBuilder builder)
    {
        builder.WithMetadata(new ResponseConfigurationPhase("configure-factory"));
    }

    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        return next;
    }
}

public static class DerivedResponseScenario
{
    public static void Map(WebApplication app)
    {
        var group = app.TryMapMediumApiNamespace<DerivedResponses.DerivedResponseGroupDefinition>();
        if (group is null) { return; }

        MapEntries(group);
        app.MapMediumApiEndpoint<DerivedResponses.DerivedProblemEndpoint>("/aliases/derived/{value:int}").WithoutDefaultEndpointName().WithTags(SharedResponseMetadata.Tag);
    }

    public static void MapEntries(RouteGroupBuilder group, bool suppressNames = false)
    {
        var entries = new[] { group.MapMediumApiEndpoint<DerivedResponses.DerivedProblemEndpoint>(), group.MapMediumApiEndpoint<DerivedResponses.InheritedDerivedResponseEndpoint>(), group.MapMediumApiEndpoint<DerivedResponses.MixedDerivedResponseEndpoint>(), group.MapMediumApiEndpoint<DerivedResponses.DerivedFormatsEndpoint>() };
        if (suppressNames)
        {
            foreach (var entry in entries) { entry.WithoutDefaultEndpointName(); }
        }
    }
}