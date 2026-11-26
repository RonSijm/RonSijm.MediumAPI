using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>
/// Adds response metadata only when the native convention finds no existing response declaration with the same status.
/// This marker does not itself declare a response or change any result, exception policy or runtime response.
/// </summary>
/// <remarks>
/// Endpoint and custom builders use an ordinary convention. Route groups use a native final convention so descendant
/// inference and endpoint conventions are visible. Existing declarations are never replaced, merged or removed.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class EndpointResponseFallbackAttribute : Attribute
{
    private readonly EndpointResponseAttribute response;

    /// <summary>Uses the same defaults, validation and immutable response values as EndpointResponseAttribute.</summary>
    public EndpointResponseFallbackAttribute(int statusCode, Type? responseType = null, string? contentType = null, params string[] additionalContentTypes)
    {
        response = new EndpointResponseAttribute(statusCode, responseType, contentType, additionalContentTypes);
    }

    /// <summary>Gets the status checked by the convention.</summary>
    public int StatusCode
    {
        get { return response.StatusCode; }
    }

    /// <summary>Gets the response type; a status-only declaration uses void.</summary>
    public Type? Type
    {
        get { return response.Type; }
    }

    /// <summary>Gets the ordered, read-only response content types.</summary>
    public IEnumerable<string> ContentTypes
    {
        get { return response.ContentTypes; }
    }

    internal void Apply(IEndpointConventionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (builder is RouteGroupBuilder)
        {
            builder.Finally(ApplyResponse);
            return;
        }

        builder.Add(ApplyResponse);
    }

    private void ApplyResponse(EndpointBuilder builder)
    {
        if (builder.Metadata.OfType<IProducesResponseTypeMetadata>().Any(metadata => metadata.StatusCode == response.StatusCode))
        {
            return;
        }

        builder.Metadata.Add(response);
    }
}