using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>Registers native endpoint filters together with any explicit MediumAPI startup conventions.</summary>
public static class MediumApiFilterExtensions
{
    /// <summary>Applies the existing combined declarations to a native builder exposed by a startup convention, then registers the filter through ASP.NET Core and returns the same builder.</summary>
    public static IEndpointConventionBuilder AddMediumApiFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilter>(this IEndpointConventionBuilder endpoint) where TFilter : class, IEndpointFilter
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        EndpointFilterConventions.Apply<TFilter>(endpoint);
        EndpointCustomMetadataDeclarations.ApplyDeclaredMetadata(typeof(TFilter), endpoint);
        EndpointAcceptsDeclarations.ApplyDeclaredAccepts(typeof(TFilter), endpoint);
        EndpointAntiforgeryConventions.ApplyDeclaredDisable(typeof(TFilter), endpoint);
        EndpointResponseDeclarations.ApplyDeclaredResponses(typeof(TFilter), endpoint);
        EndpointNativeResponseDeclarations.ApplyDeclaredResponses(typeof(TFilter), endpoint);
        EndpointResponseFallbackDeclarations.ApplyDeclaredFallbacks(typeof(TFilter), endpoint);
        MediumApiResponseHeaderExtensions.ApplyDeclaredHeaders(typeof(TFilter), endpoint);
        return endpoint.AddEndpointFilter<IEndpointConventionBuilder, TFilter>();
    }

    /// <summary>Applies optional MediumAPI conventions, declared custom metadata, request contracts, native antiforgery opt-outs, Minimal API and native MVC responses, conditional response fallbacks and response headers, then registers the filter using ASP.NET Core's native activation and pipeline.</summary>
    public static RouteHandlerBuilder AddMediumApiFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilter>(this RouteHandlerBuilder endpoint) where TFilter : class, IEndpointFilter
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        EndpointFilterConventions.Apply<TFilter>(endpoint);
        EndpointCustomMetadataDeclarations.ApplyDeclaredMetadata(typeof(TFilter), endpoint);
        EndpointAcceptsDeclarations.ApplyDeclaredAccepts(typeof(TFilter), endpoint);
        EndpointAntiforgeryConventions.ApplyDeclaredDisable(typeof(TFilter), endpoint);
        EndpointResponseDeclarations.ApplyDeclaredResponses(typeof(TFilter), endpoint);
        EndpointNativeResponseDeclarations.ApplyDeclaredResponses(typeof(TFilter), endpoint);
        EndpointResponseFallbackDeclarations.ApplyDeclaredFallbacks(typeof(TFilter), endpoint);
        MediumApiResponseHeaderExtensions.ApplyDeclaredHeaders(typeof(TFilter), endpoint);
        return endpoint.AddEndpointFilter<TFilter>();
    }

    /// <summary>Applies optional MediumAPI conventions, declared custom metadata, request contracts, native antiforgery opt-outs, Minimal API and native MVC responses, conditional response fallbacks and response headers to the group, then registers its native endpoint filter convention.</summary>
    public static RouteGroupBuilder AddMediumApiFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilter>(this RouteGroupBuilder group) where TFilter : class, IEndpointFilter
    {
        ArgumentNullException.ThrowIfNull(group);
        EndpointFilterConventions.Apply<TFilter>(group);
        EndpointCustomMetadataDeclarations.ApplyDeclaredMetadata(typeof(TFilter), group);
        EndpointAcceptsDeclarations.ApplyDeclaredAccepts(typeof(TFilter), group);
        EndpointAntiforgeryConventions.ApplyDeclaredDisable(typeof(TFilter), group);
        EndpointResponseDeclarations.ApplyDeclaredResponses(typeof(TFilter), group);
        EndpointNativeResponseDeclarations.ApplyDeclaredResponses(typeof(TFilter), group);
        EndpointResponseFallbackDeclarations.ApplyDeclaredFallbacks(typeof(TFilter), group);
        MediumApiResponseHeaderExtensions.ApplyDeclaredHeaders(typeof(TFilter), group);
        return group.AddEndpointFilter<TFilter>();
    }
}