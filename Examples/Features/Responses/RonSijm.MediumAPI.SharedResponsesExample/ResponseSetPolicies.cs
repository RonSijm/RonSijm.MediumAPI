using Microsoft.AspNetCore.Http.Metadata;

namespace RonSijm.MediumAPI.SharedResponsesExample;

public sealed class AccessResponsesAttribute : EndpointResponsesAttribute
{
    public AccessResponsesAttribute() : base(401, 403) { }
}

// Sets are authoring markers: additional interfaces do not become endpoint policies.
public sealed class MarkedResponsesAttribute : EndpointResponsesAttribute, IEndpointMetadataAttribute, ITagsMetadata, IEndpointNameMetadata
{
    public MarkedResponsesAttribute(params int[] statuses) : base(statuses) { }

    public IReadOnlyList<string> Tags => new[] { "Not an endpoint tag" };
    public string EndpointName => "TryingToOverride";
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ResponseSetPhaseAttribute : Attribute, IEndpointMetadataAttribute
{
    public ResponseSetPhaseAttribute(string phase)
    {
        Phase = phase;
    }

    public string Phase { get; }
}

public sealed record ResponseSetConfiguration(string Phase);

public sealed record ResponseSetObservation(bool ContainsAuthoringMarker);

[EndpointResponses(498)]
public abstract class ResponseSetFilterBase : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.WithMetadata(new ResponseSetConfiguration("filter-configure"));
    }

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return next(context);
    }
}

public sealed class InheritedResponseSetFilter : ResponseSetFilterBase { }

[EndpointResponse(425)]
[MarkedResponses(429, 431)]
[ResponseSetPhase("filter-custom")]
[EndpointResponse(432)]
[EndpointResponse(400, typeof(FilterError))]
public sealed class ResponseSetFilter : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.WithMetadata(new ResponseSetConfiguration("filter-configure"));
    }

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.RequestServices.GetRequiredService<SharedResponseProbe>().VisitGuard();
        if (context.GetArgument<int>(0) < 0)
        {
            return ValueTask.FromResult<object?>(TypedResults.BadRequest(new FilterError("negative", "Use a nonnegative value.")));
        }

        return next(context);
    }
}

[EndpointResponses(451)]
[MarkedResponses(452, 453)]
[ResponseSetPhase("factory-custom")]
public sealed class ResponseSetFactory : IMediumApiEndpointFilterFactory
{
    private ResponseSetFactory()
    {
        throw new InvalidOperationException("Static response-set factories are not instantiated.");
    }

    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.WithMetadata(new ResponseSetConfiguration("factory-configure"));
        endpoint.Add(builder => builder.Metadata.Add(new ResponseSetObservation(builder.Metadata.OfType<EndpointResponsesAttribute>().Any())));
    }

    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        return next;
    }
}

public static class ResponseSetScenario
{
    public static void Map(WebApplication app)
    {
        var group = app.TryMapMediumApiNamespace<ResponseSets.ResponseSetsGroupDefinition>();
        if (group is null) { return; }

        MapEntries(group);
        app.MapMediumApiEndpoint<ResponseSets.ResponseSetValueEndpoint>("/aliases/response-sets/{value:int}").WithoutDefaultEndpointName().WithTags(SharedResponseMetadata.Tag);
    }

    public static void MapEntries(RouteGroupBuilder group, bool suppressNames = false)
    {
        var endpoints = new[] { group.MapMediumApiEndpoint<ResponseSets.ResponseSetValueEndpoint>(), group.MapMediumApiEndpoint<ResponseSets.InheritedResponseSetEndpoint>(), group.MapMediumApiEndpoint<ResponseSets.TypedResponseSetEndpoint>(), group.MapMediumApiEndpoint<ResponseSets.EmptyResponseSetEndpoint>() };
        if (suppressNames)
        {
            foreach (var endpoint in endpoints) { endpoint.WithoutDefaultEndpointName(); }
        }
    }
}