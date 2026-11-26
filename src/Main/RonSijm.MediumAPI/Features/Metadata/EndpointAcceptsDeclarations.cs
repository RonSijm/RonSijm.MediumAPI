using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI;

/// <summary>Applies directly declared exact native Consumes attributes at one convention registration point without caching host state.</summary>
internal static class EndpointAcceptsDeclarations
{
    internal static void ApplyDeclaredAccepts(Type type, IEndpointConventionBuilder builder)
    {
        foreach (var declaration in type.GetCustomAttributesData())
        {
            // Preserve native constructor validation and named arguments without constructing
            // unrelated attributes or importing derived types and CLR base-class policies.
            if (declaration.AttributeType != typeof(ConsumesAttribute))
            {
                continue;
            }

            var accepts = (ConsumesAttribute)EndpointAttributeMetadata.Create(declaration);
            builder.WithMetadata(accepts);
        }
    }
}