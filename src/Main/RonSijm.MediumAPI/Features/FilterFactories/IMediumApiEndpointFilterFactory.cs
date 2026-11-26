using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace RonSijm.MediumAPI;

/// <summary>A static factory for ASP.NET Core's native endpoint filter pipeline, with optional registration conventions.</summary>
public interface IMediumApiEndpointFilterFactory
{
    /// <summary>Creates a native filter delegate from the framework's factory context and downstream delegate. ASP.NET Core controls when the factory runs.</summary>
    static abstract EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next);

    /// <summary>Applies explicit builder conventions at each registration, before declared metadata and the native factory. No factory instance is created.</summary>
    static virtual void Configure(IEndpointConventionBuilder endpoint) { }
}