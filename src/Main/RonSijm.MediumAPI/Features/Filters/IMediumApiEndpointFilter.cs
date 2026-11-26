using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace RonSijm.MediumAPI;

/// <summary>A native endpoint filter with startup conventions shared by every registration of that filter.</summary>
public interface IMediumApiEndpointFilter : IEndpointFilter
{
    /// <summary>Applies response metadata and builder conventions at registration, without request services or filter instances.</summary>
    static abstract void Configure(IEndpointConventionBuilder endpoint);
}