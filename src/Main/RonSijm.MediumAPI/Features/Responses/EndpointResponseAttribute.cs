using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;

namespace RonSijm.MediumAPI;

/// <summary>
/// Declares Minimal API response metadata using the same defaults as the framework's Produces convention.
/// This documents a response; it does not serialize, validate or change the result returned by a handler.
/// Direct class declarations are also supported on endpoints, namespace definitions and registered MediumAPI filters.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class EndpointResponseAttribute : Attribute, IProducesResponseTypeMetadata
{
    private readonly ProducesResponseTypeMetadata metadata;
    private readonly IReadOnlyList<string> contentTypes;

    /// <summary>
    /// A status-only declaration has a void response type and no content types. Supplying a response type
    /// defaults its content type to application/json. Problem responses should specify their type and media type.
    /// </summary>
    public EndpointResponseAttribute(int statusCode, Type? responseType = null, string? contentType = null, params string[] additionalContentTypes)
    {
        if (responseType is not null && string.IsNullOrEmpty(contentType))
        {
            contentType = "application/json";
        }

        string[] declaredContentTypes = contentType is null ? [] : CombineContentTypes(contentType, additionalContentTypes);
        metadata = new ProducesResponseTypeMetadata(statusCode, responseType ?? typeof(void), declaredContentTypes);
        contentTypes = Array.AsReadOnly(declaredContentTypes);
    }

    public int StatusCode => metadata.StatusCode;
    public Type? Type => metadata.Type;
    public IEnumerable<string> ContentTypes => contentTypes;

    private static string[] CombineContentTypes(string contentType, string[] additionalContentTypes)
    {
        ArgumentNullException.ThrowIfNull(additionalContentTypes);
        var combined = new string[additionalContentTypes.Length + 1];
        combined[0] = contentType;
        additionalContentTypes.CopyTo(combined, 1);
        return combined;
    }
}