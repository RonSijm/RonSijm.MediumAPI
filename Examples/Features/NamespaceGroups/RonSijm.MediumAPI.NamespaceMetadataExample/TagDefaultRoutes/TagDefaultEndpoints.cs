using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.TagDefaultRoutes
{
    [NamespaceRouteGroup("/tag-defaults")]
    [Tags(TagDefaults.Shared)]
    public sealed partial class TagDefaultsGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return context.Services.GetRequiredService<IConfiguration>().GetValue("Sample:TagDefaults", false);
        }
    }

    public sealed class GetTagDefaultStatusEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/status";

        public Ok<MetadataResponse> HandleAsync()
        {
            return TypedResults.Ok(new MetadataResponse("status"));
        }
    }

    public sealed class GetTagDefaultDetailEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/detail";

        public Ok<MetadataResponse> HandleAsync()
        {
            return TypedResults.Ok(new MetadataResponse("detail"));
        }
    }

    public sealed class GetTagDefaultDifferentEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/different";

        [Tags(TagDefaults.Different)]
        public Ok<MetadataResponse> HandleAsync()
        {
            return TypedResults.Ok(new MetadataResponse("different"));
        }
    }
}

namespace RonSijm.MediumAPI.NamespaceMetadataExample.TagDefaultRoutes.Child
{
    [NamespaceRouteGroup("/child")]
    [Tags(TagDefaults.Child)]
    public sealed partial class TagDefaultsChildGroupDefinition : INamespaceEndpointGroup;

    public sealed class GetTagDefaultChildEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/status";

        public Ok<MetadataResponse> HandleAsync()
        {
            return TypedResults.Ok(new MetadataResponse("child"));
        }
    }
}