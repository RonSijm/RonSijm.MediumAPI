namespace RonSijm.MediumAPI;

/// <summary>
/// Describes the endpoint type supplied to the current MediumAPI handler adapter.
/// This descriptive metadata is not proof of mapping origin, authorization or exposure.
/// </summary>
public sealed class MediumApiEndpointMetadata
{
    internal MediumApiEndpointMetadata(Type endpointType) => EndpointType = endpointType;

    /// <summary>The mapped endpoint type; the original handler's MethodInfo may declare a base type.</summary>
    public Type EndpointType { get; }
}