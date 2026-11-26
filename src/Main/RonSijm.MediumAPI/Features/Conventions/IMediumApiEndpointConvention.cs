using Microsoft.AspNetCore.Builder;

namespace RonSijm.MediumAPI;

/// <summary>Applies explicitly requested startup conventions to a native endpoint or group without adding a request filter.</summary>
public interface IMediumApiEndpointConvention
{
    /// <summary>Configures this registration through native builder conventions. No convention instance is created.</summary>
    static abstract void Configure(IEndpointConventionBuilder endpoint);
}