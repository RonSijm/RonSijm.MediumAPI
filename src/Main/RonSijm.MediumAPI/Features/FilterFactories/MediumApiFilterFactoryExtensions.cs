using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>Registers static endpoint filter factories with explicit MediumAPI conventions on native endpoints and groups.</summary>
public static class MediumApiFilterFactoryExtensions
{
    /// <summary>Applies the existing combined declarations to a native builder exposed by a startup convention, then registers the static factory through ASP.NET Core and returns the same builder.</summary>
    public static IEndpointConventionBuilder AddMediumApiFilterFactory<TFactory>(this IEndpointConventionBuilder endpoint) where TFactory : IMediumApiEndpointFilterFactory
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        TFactory.Configure(endpoint);
        EndpointCustomMetadataDeclarations.ApplyDeclaredMetadata(typeof(TFactory), endpoint);
        EndpointAcceptsDeclarations.ApplyDeclaredAccepts(typeof(TFactory), endpoint);
        EndpointAntiforgeryConventions.ApplyDeclaredDisable(typeof(TFactory), endpoint);
        EndpointResponseDeclarations.ApplyDeclaredResponses(typeof(TFactory), endpoint);
        EndpointNativeResponseDeclarations.ApplyDeclaredResponses(typeof(TFactory), endpoint);
        EndpointResponseFallbackDeclarations.ApplyDeclaredFallbacks(typeof(TFactory), endpoint);
        MediumApiResponseHeaderExtensions.ApplyDeclaredHeaders(typeof(TFactory), endpoint);
        return endpoint.AddEndpointFilterFactory(TFactory.Create);
    }

    /// <summary>Applies optional conventions, declared metadata, request contracts, antiforgery opt-outs, responses, fallbacks and headers, then registers the factory with ASP.NET Core. No factory instance or service registration is required.</summary>
    public static RouteHandlerBuilder AddMediumApiFilterFactory<TFactory>(this RouteHandlerBuilder endpoint) where TFactory : IMediumApiEndpointFilterFactory
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        TFactory.Configure(endpoint);
        EndpointCustomMetadataDeclarations.ApplyDeclaredMetadata(typeof(TFactory), endpoint);
        EndpointAcceptsDeclarations.ApplyDeclaredAccepts(typeof(TFactory), endpoint);
        EndpointAntiforgeryConventions.ApplyDeclaredDisable(typeof(TFactory), endpoint);
        EndpointResponseDeclarations.ApplyDeclaredResponses(typeof(TFactory), endpoint);
        EndpointNativeResponseDeclarations.ApplyDeclaredResponses(typeof(TFactory), endpoint);
        EndpointResponseFallbackDeclarations.ApplyDeclaredFallbacks(typeof(TFactory), endpoint);
        MediumApiResponseHeaderExtensions.ApplyDeclaredHeaders(typeof(TFactory), endpoint);
        return endpoint.AddEndpointFilterFactory(TFactory.Create);
    }

    /// <summary>Applies optional conventions and declared metadata in the same order as endpoint registration, then lets ASP.NET Core compose the factory for each mapped group endpoint.</summary>
    public static RouteGroupBuilder AddMediumApiFilterFactory<TFactory>(this RouteGroupBuilder group) where TFactory : IMediumApiEndpointFilterFactory
    {
        ArgumentNullException.ThrowIfNull(group);
        TFactory.Configure(group);
        EndpointCustomMetadataDeclarations.ApplyDeclaredMetadata(typeof(TFactory), group);
        EndpointAcceptsDeclarations.ApplyDeclaredAccepts(typeof(TFactory), group);
        EndpointAntiforgeryConventions.ApplyDeclaredDisable(typeof(TFactory), group);
        EndpointResponseDeclarations.ApplyDeclaredResponses(typeof(TFactory), group);
        EndpointNativeResponseDeclarations.ApplyDeclaredResponses(typeof(TFactory), group);
        EndpointResponseFallbackDeclarations.ApplyDeclaredFallbacks(typeof(TFactory), group);
        MediumApiResponseHeaderExtensions.ApplyDeclaredHeaders(typeof(TFactory), group);
        return group.AddEndpointFilterFactory(TFactory.Create);
    }
}