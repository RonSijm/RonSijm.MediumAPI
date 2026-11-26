using Microsoft.AspNetCore.Builder;

namespace RonSijm.MediumAPI;

/// <summary>Forwards directly declared opted-in attribute instances at one native convention registration point.</summary>
internal static class EndpointCustomMetadataDeclarations
{
    internal static void ApplyDeclaredMetadata(Type type, IEndpointConventionBuilder builder)
    {
        foreach (var declaration in type.GetCustomAttributesData())
        {
            if (!typeof(IEndpointMetadataAttribute).IsAssignableFrom(declaration.AttributeType) || typeof(EndpointResponseAttribute).IsAssignableFrom(declaration.AttributeType) || typeof(EndpointResponsesAttribute).IsAssignableFrom(declaration.AttributeType))
            {
                continue;
            }

            builder.WithMetadata(EndpointAttributeMetadata.Create(declaration));
        }
    }
}