using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures
{
    public sealed record ScopeResponse(string Source);

    public sealed record ScopeProviderObservation(Type EndpointType, Type? HandlerDeclaringType);

    public sealed class ScopeProviderResult : IResult, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            var identity = builder.Metadata.OfType<MediumApiEndpointMetadata>().LastOrDefault() ?? throw new InvalidOperationException("Concrete identity must be available before inferred providers execute.");
            builder.Metadata.Add(new ScopeProviderObservation(identity.EndpointType, method.DeclaringType));
        }

        public Task ExecuteAsync(HttpContext context) => TypedResults.Ok(new ScopeResponse("provider")).ExecuteAsync(context);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ThrowingAuthorizationAttribute : Attribute, IAuthorizeData
    {
        public ThrowingAuthorizationAttribute() => throw new InvalidOperationException("Skipped declarations must not construct metadata.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }
}

namespace RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Inherited
{
    public abstract class ProviderHandlerBase
    {
        public ScopeProviderResult HandleAsync() => new();
    }

    public sealed class ProviderEndpoint : ProviderHandlerBase, IGetEndpoint, IManuallyMappedEndpoint
    {
        public ProviderEndpoint() => throw new InvalidOperationException("Metadata capture must not activate endpoint targets.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/provider";
    }

    public abstract class LegacyHandlerBase : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/legacy";

        public Ok<ScopeResponse> HandleAsync() => TypedResults.Ok(new ScopeResponse("legacy"));
    }

    public sealed class LegacyEndpoint : LegacyHandlerBase, IManuallyMappedEndpoint { }
}

namespace RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Fresh
{
    public abstract class SharedHandlerBase
    {
        public Ok<ScopeResponse> HandleAsync() => TypedResults.Ok(new ScopeResponse("fresh"));
    }

    public sealed class FreshEndpoint : SharedHandlerBase, IGetEndpoint, IManuallyMappedEndpoint
    {
        public FreshEndpoint() => throw new InvalidOperationException("Inspecting aliases must not activate endpoint targets.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/fresh";
    }

    public sealed class OtherFreshEndpoint : SharedHandlerBase, IGetEndpoint, IManuallyMappedEndpoint
    {
        public OtherFreshEndpoint() => throw new InvalidOperationException("Inspecting concrete types must not activate endpoint targets.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/other-fresh";
    }
}

namespace RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Manual
{
    [ThrowingAuthorization]
    public sealed class ManualThrowingEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public ManualThrowingEndpoint() => throw new InvalidOperationException("Manifest inspection must not activate endpoint targets.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/manual";

        public Ok<ScopeResponse> HandleAsync() => throw new InvalidOperationException("Skipped endpoints must not execute handlers.");
    }
}

namespace RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Scoped
{
    [Host("group.scope.test")]
    public sealed class ScopedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/scoped");
    }

    public sealed class ScopedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<ScopeResponse> HandleAsync() => TypedResults.Ok(new ScopeResponse("scoped"));
    }
}

namespace RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Composed
{
    [Host("parent.scope.test")]
    public sealed class ParentGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-parent");
    }
}

namespace RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Composed.Child
{
    public sealed class ChildGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-child");
    }

    public sealed class ChildEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<ScopeResponse> HandleAsync() => TypedResults.Ok(new ScopeResponse("child"));
    }
}

namespace RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Disabled
{
    [Host("disabled.scope.test")]
    [ThrowingAuthorization]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled groups must not map.");
    }

    [ThrowingAuthorization]
    public sealed class DisabledEndpoint : IGetEndpoint
    {
        public DisabledEndpoint() => throw new InvalidOperationException("Disabled targets must not be constructed.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled";

        public Ok<ScopeResponse> HandleAsync() => throw new InvalidOperationException("Disabled handlers must not execute.");
    }

    public sealed class DisabledAliasEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/disabled-alias";

        public Ok<ScopeResponse> HandleAsync() => TypedResults.Ok(new ScopeResponse("disabled-alias"));
    }
}

namespace RonSijm.MediumAPI.EndpointIdentityExample.IntegrationTest.ScopeFixtures.Disabled.Child
{
    [ThrowingAuthorization]
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("Disabled ancestors must suppress descendant conditions.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled descendants must not map.");
    }

    [ThrowingAuthorization]
    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        public DisabledChildEndpoint() => throw new InvalidOperationException("Disabled descendant targets must not be constructed.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-child";

        public Ok<ScopeResponse> HandleAsync() => throw new InvalidOperationException("Disabled descendant handlers must not execute.");
    }
}