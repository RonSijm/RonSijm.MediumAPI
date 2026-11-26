using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest
{
    public sealed record VisibilityMetadata(bool ExcludeFromDescription) : IExcludeFromDescriptionMetadata;

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedTagsAttribute : Attribute, ITagsMetadata
    {
        public UnrelatedTagsAttribute() => throw new InvalidOperationException("Unselected tag attributes must not be constructed.");
        public IReadOnlyList<string> Tags => throw new InvalidOperationException("Unselected tag getters must not execute.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedGroupNameAttribute : Attribute, IEndpointGroupNameMetadata
    {
        public UnrelatedGroupNameAttribute() => throw new InvalidOperationException("Unselected document-group attributes must not be constructed.");
        public string EndpointGroupName => throw new InvalidOperationException("Unselected document-group getters must not execute.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedExclusionAttribute : Attribute, IExcludeFromDescriptionMetadata
    {
        public UnrelatedExclusionAttribute() => throw new InvalidOperationException("Unselected exclusion attributes must not be constructed.");
        public bool ExcludeFromDescription => throw new InvalidOperationException("Unselected exclusion getters must not execute.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ThrowingAuthorizationAttribute : Attribute, IAuthorizeData
    {
        public ThrowingAuthorizationAttribute() => throw new InvalidOperationException("Skipped endpoint metadata must not be materialized.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }

    public sealed class ScopeMetadataResult : IResult, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add(new TagsAttribute("scope-provider"));
            builder.Metadata.Add(new EndpointGroupNameAttribute("scope-provider"));
            builder.Metadata.Add(new VisibilityMetadata(false));
        }

        public Task ExecuteAsync(HttpContext context) => TypedResults.Ok(new DocumentationResponse("ordered")).ExecuteAsync(context);
    }
}

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Ordered
{
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/scope/ordered").WithTags("scope-group").WithGroupName("scope-group").WithMetadata(new VisibilityMetadata(true));
            ((IEndpointConventionBuilder)group).Finally(builder =>
            {
                builder.Metadata.Add(new TagsAttribute("scope-group-final"));
                builder.Metadata.Add(new EndpointGroupNameAttribute(DocumentationGroups.Public));
                builder.Metadata.Add(new VisibilityMetadata(false));
            });
            return group;
        }
    }

    [Tags("scope-class", "scope-second")]
    [EndpointGroupName("scope-class")]
    [ExcludeFromDescription]
    public sealed class OrderedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [Tags("scope-method")]
        [EndpointGroupName("scope-method")]
        [ExcludeFromDescription]
        public ScopeMetadataResult HandleAsync() => new();

        public static void Configure(RouteHandlerBuilder endpoint)
        {
            endpoint.WithTags("scope-configured").WithGroupName("scope-configured").WithMetadata(new VisibilityMetadata(false));
            endpoint.Finally(builder =>
            {
                builder.Metadata.Add(new TagsAttribute("scope-endpoint-final"));
                builder.Metadata.Add(new EndpointGroupNameAttribute("scope-endpoint-final"));
                builder.Metadata.Add(new VisibilityMetadata(false));
            });
        }
    }
}

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Inheritance
{
    [Tags("base-class")]
    [EndpointGroupName("base-class")]
    [ExcludeFromDescription]
    public abstract class BaseClassEndpoint
    {
        public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("base-class-ignored"));
    }

    public sealed class BaseClassIgnoredEndpoint : BaseClassEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/base-class-ignored";
    }

    [Tags("base-class")]
    [EndpointGroupName("base-class")]
    [ExcludeFromDescription]
    public abstract class BaseMethodEndpoint
    {
        [Tags(DocumentationTags.Method)]
        [EndpointGroupName(DocumentationGroups.Admin)]
        [ExcludeFromDescription]
        public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("inherited-method"));
    }

    [Tags(DocumentationTags.Catalog)]
    [EndpointGroupName(DocumentationGroups.Public)]
    public sealed class InheritedMethodEndpoint : BaseMethodEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/inherited-method";
    }
}

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Selected
{
    [Tags(DocumentationTags.Catalog)]
    [EndpointGroupName(DocumentationGroups.Public)]
    [UnrelatedTags]
    [UnrelatedGroupName]
    [UnrelatedExclusion]
    public sealed class IgnoredAttributesEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/ignored-attributes";

        public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("ignored-attributes"));
    }

    [Tags(DocumentationTags.Catalog, DocumentationTags.Read)]
    [EndpointGroupName(DocumentationGroups.Public)]
    [ExcludeFromDescription]
    public sealed class FreshMetadataEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/fresh";

        public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("fresh"));
    }

    [Tags]
    [EndpointGroupName(DocumentationGroups.Public)]
    public sealed class EmptyTagsEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/empty-tags";

        public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("empty-tags"));
    }

    [Tags(DocumentationTags.Catalog)]
    [EndpointGroupName(DocumentationGroups.Public)]
    public sealed class ActivationEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public ActivationEndpoint(DocumentationProbe probe)
        {
            probe.Record("activation-construction");
            throw new InvalidOperationException("Document generation must not construct endpoint instances.");
        }

        [StringSyntax("Route")]
        public string Pattern => "/scope/activation";

        public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("activation"));
    }
}

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Scoped
{
    [Tags(DocumentationTags.Group)]
    [EndpointGroupName(DocumentationGroups.Admin)]
    [ExcludeFromDescription]
    public sealed class ScopedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/scoped");
    }

    [Tags(DocumentationTags.Catalog)]
    [EndpointGroupName(DocumentationGroups.Public)]
    public sealed class ScopedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("scoped"));
    }
}

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Composed
{
    [Tags(DocumentationTags.Group)]
    [EndpointGroupName(DocumentationGroups.Admin)]
    [ExcludeFromDescription]
    public sealed class ParentGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-parent");
    }
}

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Composed.Child
{
    public sealed class ChildGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-child");
    }

    [Tags(DocumentationTags.Catalog)]
    [EndpointGroupName(DocumentationGroups.Public)]
    public sealed class ChildEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("child"));
    }
}

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Disabled
{
    [Tags(DocumentationTags.Group)]
    [EndpointGroupName(DocumentationGroups.Admin)]
    [ExcludeFromDescription]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled definitions must not map.");
    }

    [Tags(DocumentationTags.Catalog)]
    [EndpointGroupName(DocumentationGroups.Public)]
    [ExcludeFromDescription]
    [ThrowingAuthorization]
    public sealed class DisabledEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled";

        public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("disabled"));
    }

    [Tags(DocumentationTags.Read)]
    [EndpointGroupName(DocumentationGroups.Public)]
    public sealed class DisabledAliasEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-alias";

        public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("disabled-alias"));
    }
}

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Disabled.Child
{
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("Disabled descendants must not evaluate conditions.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled descendants must not map.");
    }

    [Tags(DocumentationTags.Read)]
    [EndpointGroupName(DocumentationGroups.Public)]
    [ExcludeFromDescription]
    [ThrowingAuthorization]
    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-child";

        public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("disabled-child"));
    }
}

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Manual
{
    [Tags(DocumentationTags.Catalog)]
    [EndpointGroupName(DocumentationGroups.Public)]
    [ExcludeFromDescription]
    [ThrowingAuthorization]
    public sealed class ManualThrowingEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/manual";

        public Ok<DocumentationResponse> HandleAsync(DocumentationProbe probe) => TypedResults.Ok(probe.Record("manual"));
    }
}