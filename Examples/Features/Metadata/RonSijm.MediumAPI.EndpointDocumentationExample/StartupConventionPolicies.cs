using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.EndpointDocumentationExample;

public sealed record SamplePresentation(bool UsesJsonResponses);

public sealed record SampleTokenResponse(string Header, string Token);

public sealed record SampleAuthenticationResponse(string Status, string? Redirect);

public sealed record ConventionMarker(string Value);

public sealed class StartupConventionProbe
{
    public int Constructions { get; set; }
    public int Calls { get; set; }
}

public static class StartupConventionSample
{
    public static void Map(WebApplication app)
    {
        var presentation = new SamplePresentation(app.Configuration.GetValue("Sample:JsonResponses", true));
        var group = app.TryMapMediumApiNamespace<ConventionEndpoints.ConventionGroupDefinition>(builder => builder.WithMetadata(presentation));
        if (group is null) { return; }

        group.MapMediumApiEndpoint<ConventionEndpoints.ConventionAuthenticationEndpoint>("/authenticate-alias").WithoutDefaultEndpointName();
        app.MapMediumApiEndpoint<ConventionEndpoints.ConventionWorkspaceEndpoint>().WithGroupName(DocumentationGroups.Public).WithTags("Startup conventions").WithMetadata(presentation).AddMediumApiConvention<WorkspaceResponsesConvention>();
    }
}

// The host selects presentation; this helper only appends ordinary native metadata.
public static class SampleJsonResponses
{
    public static void Add(IEndpointConventionBuilder builder, Action<EndpointBuilder> responses)
    {
        builder.AddConditionalConvention(endpoint => endpoint.Metadata.OfType<SamplePresentation>().LastOrDefault()?.UsesJsonResponses == true, responses);
    }

    public static void Produces(EndpointBuilder endpoint, int status, Type? type = null, string? contentType = null)
    {
        endpoint.Metadata.Add(new ProducesResponseTypeMetadata(status, type ?? typeof(void), contentType is null ? Array.Empty<string>() : new[] { contentType }));
    }

    public static void Problem(EndpointBuilder endpoint, int status)
    {
        Produces(endpoint, status, typeof(ProblemDetails), "application/problem+json");
    }
}

public sealed class TokenResponsesConvention : IMediumApiEndpointConvention
{
    private TokenResponsesConvention()
    {
        throw new InvalidOperationException("A startup convention is never instantiated.");
    }

    public static void Configure(IEndpointConventionBuilder builder)
    {
        SampleJsonResponses.Add(builder, endpoint => SampleJsonResponses.Produces(endpoint, 200, typeof(SampleTokenResponse), "application/json"));
    }
}

public sealed class AuthenticationResponsesConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        SampleJsonResponses.Add(builder, endpoint =>
        {
            SampleJsonResponses.Produces(endpoint, 200, typeof(SampleAuthenticationResponse), "application/json");
            SampleJsonResponses.Problem(endpoint, 400);
            SampleJsonResponses.Problem(endpoint, 401);
        });
    }
}

public sealed class RequestOptionsResponsesConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        SampleJsonResponses.Add(builder, endpoint =>
        {
            SampleJsonResponses.Produces(endpoint, 200, contentType: "application/json");
            SampleJsonResponses.Problem(endpoint, 400);
        });
    }
}

public sealed class ChallengeResponsesConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        SampleJsonResponses.Add(builder, endpoint =>
        {
            SampleJsonResponses.Produces(endpoint, 302);
            SampleJsonResponses.Problem(endpoint, 400);
        });
    }
}

public sealed class LogoutResponsesConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        SampleJsonResponses.Add(builder, endpoint =>
        {
            SampleJsonResponses.Produces(endpoint, 204);
            SampleJsonResponses.Problem(endpoint, 400);
        });
    }
}

public readonly struct WorkspaceResponsesConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        SampleJsonResponses.Add(builder, endpoint =>
        {
            foreach (var status in new[] { 204, 400, 401, 404 }) { SampleJsonResponses.Produces(endpoint, status); }
        });
    }
}

public sealed class DocumentScopeConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        builder.Add(endpoint => endpoint.Metadata.Add(new ConventionMarker("group")));
    }
}