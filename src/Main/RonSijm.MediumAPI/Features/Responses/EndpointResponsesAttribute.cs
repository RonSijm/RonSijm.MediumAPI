namespace RonSijm.MediumAPI;

/// <summary>
/// Declares an ordered set of Minimal API responses at one supported declaration slot.
/// This authoring marker is replaced by its response objects; additional interfaces on derived markers are not forwarded.
/// Native mappings and arbitrary WithMetadata calls are not supported declaration-expansion APIs.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class EndpointResponsesAttribute : Attribute
{
    /// <summary>Copies the supplied statuses into ordinary response declarations, preserving order and duplicates. An empty set is valid.</summary>
    public EndpointResponsesAttribute(params int[] statusCodes)
    {
        ArgumentNullException.ThrowIfNull(statusCodes);
        Responses = Array.AsReadOnly(Array.ConvertAll(statusCodes, statusCode => new EndpointResponseAttribute(statusCode)));
    }

    /// <summary>Copies list membership while preserving existing response objects, order and duplicates. Null entries are not allowed; an empty list is valid.</summary>
    protected EndpointResponsesAttribute(IReadOnlyList<EndpointResponseAttribute> responses)
    {
        ArgumentNullException.ThrowIfNull(responses);
        var responseSnapshot = responses.ToArray();
        if (Array.Exists(responseSnapshot, response => response is null))
        {
            throw new ArgumentException("Response declarations cannot contain null entries.", nameof(responses));
        }

        Responses = Array.AsReadOnly(responseSnapshot);
    }

    /// <summary>Read-only response declarations. The status-code constructor supplies void response types and no content types.</summary>
    public IReadOnlyList<EndpointResponseAttribute> Responses { get; }
}