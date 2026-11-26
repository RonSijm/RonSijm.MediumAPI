using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>Applies explicitly declared policies, request body size limits, form limits, request timeouts, host routing, antiforgery, rate limiting, output caching, documentation metadata and API Explorer groups, Minimal API and native MVC response declarations, response headers, endpoint filter attributes and explicitly opted-in custom metadata to one enabled namespace mount.</summary>
internal static class NamespaceGroupMetadata
{
    internal static void Apply(Type definitionType, RouteGroupBuilder group)
    {
        // Read declarations without constructing unrelated attributes or inheriting base-class policies.
        // Each enabled mapping operation gets its own instances; no metadata or host state is cached.
        foreach (var declaration in definitionType.GetCustomAttributesData())
        {
            if (!IsSupported(declaration.AttributeType))
            {
                continue;
            }

            var metadata = EndpointAttributeMetadata.Create(declaration);
            if (metadata is EndpointFilterAttribute filter)
            {
                filter.Apply(group);
            }
            else if (metadata is EndpointConventionAttribute convention)
            {
                convention.Apply(group);
            }
            else if (metadata is DisableEndpointAntiforgeryAttribute disable)
            {
                disable.Apply(group);
            }
            else if (metadata is EndpointResponseFallbackAttribute fallback)
            {
                fallback.Apply(group);
            }
            else if (metadata is ResponseHeaderAttribute header)
            {
                group.WithResponseHeader(header);
            }
            else if (metadata is ResponseHeadersAttribute headers)
            {
                group.WithResponseHeaders(headers);
            }
            else if (metadata is EndpointResponsesAttribute responses)
            {
                foreach (var response in responses.Responses)
                {
                    group.WithMetadata(response);
                }
            }
            else
            {
                group.WithMetadata(metadata);
            }
        }
    }

    private static bool IsSupported(Type attributeType)
    {
        return typeof(IAuthorizeData).IsAssignableFrom(attributeType)
               || typeof(IAllowAnonymous).IsAssignableFrom(attributeType)
               || typeof(EndpointFilterAttribute).IsAssignableFrom(attributeType)
               || typeof(EndpointConventionAttribute).IsAssignableFrom(attributeType)
               || attributeType == typeof(HostAttribute)
               || attributeType == typeof(RequestTimeoutAttribute)
               || attributeType == typeof(DisableRequestTimeoutAttribute)
               || attributeType == typeof(RequestSizeLimitAttribute)
               || attributeType == typeof(DisableRequestSizeLimitAttribute)
               || attributeType == typeof(RequestFormLimitsAttribute)
               || attributeType == typeof(TagsAttribute)
               || attributeType == typeof(EndpointGroupNameAttribute)
               || attributeType == typeof(ExcludeFromDescriptionAttribute)
               || typeof(ResponseHeaderDeclarationAttribute).IsAssignableFrom(attributeType)
               || typeof(EndpointResponseAttribute).IsAssignableFrom(attributeType)
               || typeof(EndpointResponsesAttribute).IsAssignableFrom(attributeType)
               || attributeType == typeof(EndpointResponseFallbackAttribute)
               || attributeType == typeof(ConsumesAttribute)
               || attributeType == typeof(DisableEndpointAntiforgeryAttribute)
               || attributeType == typeof(RequireAntiforgeryTokenAttribute)
               || attributeType == typeof(OutputCacheAttribute)
               || attributeType == typeof(EnableRateLimitingAttribute)
               || attributeType == typeof(DisableRateLimitingAttribute)
               || attributeType == typeof(EnableCorsAttribute)
               || attributeType == typeof(DisableCorsAttribute)
               || EndpointNativeResponseDeclarations.IsSupported(attributeType)
               || typeof(IEndpointMetadataAttribute).IsAssignableFrom(attributeType);
    }
}