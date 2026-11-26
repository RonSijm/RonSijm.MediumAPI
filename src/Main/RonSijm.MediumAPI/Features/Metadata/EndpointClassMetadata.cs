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

/// <summary>Adds supported authorization, native CORS, output-cache, rate-limiting, antiforgery, documentation, host-routing, request-timeout, request body size limit, form limit, Minimal API and native MVC response and explicitly opted-in custom metadata attributes declared directly on the mapped endpoint type to one native endpoint builder.</summary>
internal static class EndpointClassMetadata
{
    internal static void Apply(Type endpointType, EndpointBuilder builder)
    {
        // Do not construct unrelated attributes or import CLR base-class policies.
        // Native endpoint materialization owns each fresh metadata object; none is cached globally.
        foreach (var declaration in endpointType.GetCustomAttributesData())
        {
            if (!typeof(IAuthorizeData).IsAssignableFrom(declaration.AttributeType) && !typeof(IAllowAnonymous).IsAssignableFrom(declaration.AttributeType) && declaration.AttributeType != typeof(EnableCorsAttribute) && declaration.AttributeType != typeof(DisableCorsAttribute) && declaration.AttributeType != typeof(OutputCacheAttribute) && declaration.AttributeType != typeof(EnableRateLimitingAttribute) && declaration.AttributeType != typeof(DisableRateLimitingAttribute) && declaration.AttributeType != typeof(RequireAntiforgeryTokenAttribute) && declaration.AttributeType != typeof(TagsAttribute) && declaration.AttributeType != typeof(EndpointGroupNameAttribute) && declaration.AttributeType != typeof(ExcludeFromDescriptionAttribute) && declaration.AttributeType != typeof(HostAttribute) && declaration.AttributeType != typeof(RequestTimeoutAttribute) && declaration.AttributeType != typeof(DisableRequestTimeoutAttribute) && declaration.AttributeType != typeof(RequestSizeLimitAttribute) && declaration.AttributeType != typeof(DisableRequestSizeLimitAttribute) && declaration.AttributeType != typeof(RequestFormLimitsAttribute) && !typeof(EndpointResponseAttribute).IsAssignableFrom(declaration.AttributeType) && !typeof(EndpointResponsesAttribute).IsAssignableFrom(declaration.AttributeType) && declaration.AttributeType != typeof(ConsumesAttribute) && !EndpointNativeResponseDeclarations.IsSupported(declaration.AttributeType) && !typeof(IEndpointMetadataAttribute).IsAssignableFrom(declaration.AttributeType))
            {
                continue;
            }

            var metadata = EndpointAttributeMetadata.Create(declaration);
            if (metadata is EndpointResponsesAttribute responses)
            {
                foreach (var response in responses.Responses)
                {
                    builder.Metadata.Add(response);
                }
            }
            else
            {
                builder.Metadata.Add(metadata);
            }
        }
    }
}