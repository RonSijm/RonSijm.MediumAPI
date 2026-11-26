using System.Reflection;
using Microsoft.AspNetCore.Builder;

namespace RonSijm.MediumAPI;

/// <summary>Applies directly declared responses at one native convention registration point without caching host state.</summary>
internal static class EndpointResponseDeclarations
{
    internal static void ApplyDeclaredResponses(Type type, IEndpointConventionBuilder builder)
    {
        var declarations = type.GetCustomAttributesData();
        if (!declarations.Any(declaration => typeof(EndpointResponsesAttribute).IsAssignableFrom(declaration.AttributeType)))
        {
            foreach (var response in type.GetCustomAttributes<EndpointResponseAttribute>(inherit: false))
            {
                builder.WithMetadata(response);
            }

            return;
        }

        foreach (var declaration in declarations)
        {
            if (!typeof(EndpointResponseAttribute).IsAssignableFrom(declaration.AttributeType) && !typeof(EndpointResponsesAttribute).IsAssignableFrom(declaration.AttributeType))
            {
                continue;
            }

            var metadata = EndpointAttributeMetadata.Create(declaration);
            if (metadata is EndpointResponsesAttribute responses)
            {
                foreach (var response in responses.Responses)
                {
                    builder.WithMetadata(response);
                }
            }
            else
            {
                builder.WithMetadata(metadata);
            }
        }
    }

    internal static void ApplyMethodResponseSets(MethodInfo method, RouteHandlerBuilder endpoint)
    {
        if (method.IsDefined(typeof(EndpointResponsesAttribute), inherit: true))
        {
            endpoint.Add(ExpandResponseSets);
        }
    }

    private static void ExpandResponseSets(EndpointBuilder builder)
    {
        for (var index = 0; index < builder.Metadata.Count; index++)
        {
            if (builder.Metadata[index] is not EndpointResponsesAttribute responses)
            {
                continue;
            }

            builder.Metadata.RemoveAt(index);
            for (var responseIndex = 0; responseIndex < responses.Responses.Count; responseIndex++)
            {
                builder.Metadata.Insert(index + responseIndex, responses.Responses[responseIndex]);
            }

            index += responses.Responses.Count - 1;
        }
    }
}