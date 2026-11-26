using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.CustomMetadataExample.Endpoints.Manual
{
    [AppMetadata("manual")]
    public sealed class AliasMetadataEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/unmapped";

        public Ok<MetadataReply> HandleAsync(HttpContext context)
        {
            return MetadataReplies.Ok("alias", context);
        }
    }
}

namespace RonSijm.MediumAPI.CustomMetadataExample.Endpoints.Api.Disabled
{
    [SkippedMetadata]
    public sealed class DisabledMetadataDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return false;
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            throw new InvalidOperationException("Disabled namespace definitions must not map.");
        }
    }

    [SkippedMetadata]
    public sealed class DisabledMetadataEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/disabled";

        public string HandleAsync()
        {
            throw new InvalidOperationException("Disabled endpoints must not activate.");
        }
    }
}