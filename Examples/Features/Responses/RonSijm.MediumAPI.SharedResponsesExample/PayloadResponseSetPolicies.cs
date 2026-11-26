namespace RonSijm.MediumAPI.SharedResponsesExample;

public sealed class PayloadProblemResponsesAttribute : EndpointResponsesAttribute
{
    public PayloadProblemResponsesAttribute(params int[] statuses) : base(CreateResponses(statuses)) { }

    private static IReadOnlyList<EndpointResponseAttribute> CreateResponses(int[] statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        return statuses.Select(status => new ProblemResponseAttribute(status)).ToArray();
    }
}

public sealed class FormatResponseAttribute : EndpointResponseAttribute
{
    public FormatResponseAttribute(string format, int status, Type type, string contentType, params string[] additionalContentTypes) : base(status, type, contentType, additionalContentTypes)
    {
        Format = format;
    }

    public string Format { get; }
}

public sealed class FormatResponsesAttribute : EndpointResponsesAttribute
{
    public FormatResponsesAttribute() : base(CreateResponses()) { }

    private static IReadOnlyList<EndpointResponseAttribute> CreateResponses()
    {
        var text = new FormatResponseAttribute("text", 418, typeof(string), "text/plain", "text/csv");
        var structured = new FormatResponseAttribute("structured", 419, typeof(FilterError), "application/xml", "application/json");
        return new EndpointResponseAttribute[] { text, structured, text };
    }
}

public sealed class EmptyPayloadResponsesAttribute : EndpointResponsesAttribute
{
    public EmptyPayloadResponsesAttribute() : base(Array.Empty<EndpointResponseAttribute>()) { }
}

[EndpointResponse(425)]
[PayloadProblemResponses(429, 431)]
[ResponseSetPhase("payload-filter-custom")]
[EndpointResponse(432)]
public sealed class PayloadResponseFilter : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.WithMetadata(new ResponseSetConfiguration("payload-filter-configure"));
    }

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.RequestServices.GetRequiredService<SharedResponseProbe>().VisitGuard();
        if (context.GetArgument<int>(0) < 0)
        {
            return ValueTask.FromResult<object?>(TypedResults.Problem(statusCode: 400, title: "Use a nonnegative value.", type: "urn:sample:payload-negative"));
        }

        return next(context);
    }
}

[EndpointResponse(451)]
[PayloadProblemResponses(452, 453)]
[ResponseSetPhase("payload-factory-custom")]
public sealed class PayloadResponseFactory : IMediumApiEndpointFilterFactory
{
    private PayloadResponseFactory()
    {
        throw new InvalidOperationException("Payload response factories are static declarations.");
    }

    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.WithMetadata(new ResponseSetConfiguration("payload-factory-configure"));
    }

    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        return next;
    }
}

public static class PayloadResponseSetScenario
{
    public static void Map(WebApplication app)
    {
        var group = app.TryMapMediumApiNamespace<PayloadResponseSets.PayloadResponseSetsGroupDefinition>();
        if (group is null) { return; }

        MapEntries(group);
        app.MapMediumApiEndpoint<PayloadResponseSets.PayloadResponseEndpoint>("/aliases/payload-response-sets/{value:int}").WithoutDefaultEndpointName().WithTags(SharedResponseMetadata.Tag);
    }

    public static void MapEntries(RouteGroupBuilder group, bool suppressNames = false)
    {
        var endpoints = new[] { group.MapMediumApiEndpoint<PayloadResponseSets.PayloadResponseEndpoint>(), group.MapMediumApiEndpoint<PayloadResponseSets.MixedPayloadResponseEndpoint>(), group.MapMediumApiEndpoint<PayloadResponseSets.EmptyPayloadResponseEndpoint>() };
        if (suppressNames)
        {
            foreach (var endpoint in endpoints) { endpoint.WithoutDefaultEndpointName(); }
        }
    }
}