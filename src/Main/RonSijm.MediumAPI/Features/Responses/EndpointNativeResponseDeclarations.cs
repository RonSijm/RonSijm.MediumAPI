using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI;

/// <summary>Forwards directly declared exact native MVC response providers without converting them to Minimal API response metadata.</summary>
internal static class EndpointNativeResponseDeclarations
{
    internal static bool IsSupported(Type attributeType)
    {
        return attributeType == typeof(ProducesResponseTypeAttribute)
               || attributeType.IsConstructedGenericType && !attributeType.ContainsGenericParameters && attributeType.GetGenericTypeDefinition() == typeof(ProducesResponseTypeAttribute<>);
    }

    internal static void ApplyDeclaredResponses(Type type, IEndpointConventionBuilder builder)
    {
        foreach (var declaration in type.GetCustomAttributesData())
        {
            if (!IsSupported(declaration.AttributeType))
            {
                continue;
            }

            builder.WithMetadata(EndpointAttributeMetadata.Create(declaration));
        }
    }
}