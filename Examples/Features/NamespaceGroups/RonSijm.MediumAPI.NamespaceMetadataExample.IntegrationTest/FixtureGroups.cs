using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest.Fixtures
{
    public sealed record StageMarker(string Stage);

    // Implementing documentation interfaces does not opt arbitrary attributes into
    // the concrete native-attribute whitelist, and their constructors must not run.
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedDocumentationAttribute : Attribute, ITagsMetadata, IEndpointSummaryMetadata, IEndpointDescriptionMetadata, IExcludeFromDescriptionMetadata
    {
        public UnrelatedDocumentationAttribute() => throw new InvalidOperationException("Unrelated documentation attributes must not be constructed.");
        public IReadOnlyList<string> Tags => ["unrelated"];
        public string Summary => "unrelated";
        public string Description => "unrelated";
        public bool ExcludeFromDescription => true;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisabledConstructorAttribute : Attribute, IAuthorizeData
    {
        public DisabledConstructorAttribute() => throw new InvalidOperationException("A disabled namespace must not construct supported attributes.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }
}

namespace RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest.Fixtures.Probe
{
    [Tags("attribute-first", "attribute-second")]
    [UnrelatedDocumentation]
    public sealed class ProbeGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/probe").WithTags("map").WithSummary("map-summary").WithDescription("map-description").WithMetadata(new StageMarker("Map"));
    }

    public sealed class ProbeEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<MetadataResponse> HandleAsync() => TypedResults.Ok(new MetadataResponse("probe"));
        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithMetadata(new StageMarker("Endpoint"));
    }
}

namespace RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest.Fixtures.Disabled
{
    [Tags("disabled")]
    [ExcludeFromDescription]
    [DisabledConstructor]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled Map must not run.");
    }

    public sealed class DisabledEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/disabled-parent";

        public Ok<MetadataResponse> HandleAsync() => TypedResults.Ok(new MetadataResponse("disabled"));
    }
}

namespace RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest.Fixtures.Disabled.Child
{
    [Tags("disabled-child")]
    [ExcludeFromDescription]
    [DisabledConstructor]
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("A disabled parent must suppress its child's condition.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("A disabled parent must suppress its child's Map.");
    }

    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/disabled-child";

        public Ok<MetadataResponse> HandleAsync() => TypedResults.Ok(new MetadataResponse("disabled-child"));
    }
}