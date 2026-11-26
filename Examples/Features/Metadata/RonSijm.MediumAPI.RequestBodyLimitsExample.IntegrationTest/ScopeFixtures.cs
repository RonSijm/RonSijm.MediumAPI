using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DerivedBodyLimitAttribute : RequestSizeLimitAttribute
    {
        public DerivedBodyLimitAttribute() : base(1) => throw new InvalidOperationException("The selective reader must not construct derived request-size attributes.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DerivedDisableBodyLimitAttribute : DisableRequestSizeLimitAttribute
    {
        public DerivedDisableBodyLimitAttribute() => throw new InvalidOperationException("The selective reader must not construct derived disable attributes.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedBodyLimitAttribute : Attribute
    {
        public UnrelatedBodyLimitAttribute() => throw new InvalidOperationException("Unrelated attribute construction is outside request-size forwarding.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ThrowingBodyAuthorizationAttribute : Attribute, IAuthorizeData
    {
        public ThrowingBodyAuthorizationAttribute() => throw new InvalidOperationException("Skipped supported metadata must not be constructed.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }

    public sealed class ScopeLimitResult : IResult, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) => builder.Metadata.Add(new RequestSizeLimitAttribute(1400));
        public Task ExecuteAsync(HttpContext context) => TypedResults.NoContent().ExecuteAsync(context);
    }

    public sealed record ScopeBodyResponse(long Bytes, bool HasFeature, long? MaxRequestBodySize);

    public sealed class ScopeBodyProbe
    {
        public int Calls { get; private set; }
        public void Record() => Calls++;
    }

    internal static class ScopeBodyReplies
    {
        internal static async Task<Ok<ScopeBodyResponse>> ReadAsync(HttpContext context, ScopeBodyProbe probe, CancellationToken cancellationToken)
        {
            using var body = new MemoryStream();
            await context.Request.Body.CopyToAsync(body, cancellationToken);
            probe.Record();
            var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            return TypedResults.Ok(new ScopeBodyResponse(body.Length, feature is not null, feature?.MaxRequestBodySize));
        }
    }
}

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Ordered
{
    [RequestSizeLimit(1100, Order = int.MaxValue)]
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/scope/ordered").WithMetadata(new RequestSizeLimitAttribute(1000));
            ((IEndpointConventionBuilder)group).Finally(builder => builder.Metadata.Add(new RequestSizeLimitAttribute(1800)));
            return group;
        }
    }

    [RequestSizeLimit(1300, Order = int.MinValue)]
    public sealed class OrderedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [RequestSizeLimit(1500, Order = -1000)]
        public ScopeLimitResult HandleAsync() => new();

        public static void Configure(RouteHandlerBuilder endpoint)
        {
            endpoint.WithMetadata(new RequestSizeLimitAttribute(1600));
            endpoint.Finally(builder => builder.Metadata.Add(new RequestSizeLimitAttribute(1700)));
        }
    }
}

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Inheritance
{
    [RequestSizeLimit(1000)]
    [DisableRequestSizeLimit]
    public abstract class GroupBase { }

    public sealed class BaseGroupIgnoredDefinition : GroupBase, INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/base-group");
    }

    public sealed class BaseGroupValueEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }

    [RequestSizeLimit(1000)]
    [DisableRequestSizeLimit]
    public abstract class BaseClassEndpoint
    {
        public NoContent HandleAsync() => TypedResults.NoContent();
    }

    public sealed class BaseClassIgnoredEndpoint : BaseClassEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/base-class-ignored";
    }

    [DisableRequestSizeLimit]
    public abstract class BaseMethodEndpoint
    {
        [RequestSizeLimit(1500)]
        public NoContent HandleAsync() => TypedResults.NoContent();
    }

    [RequestSizeLimit(1300)]
    public sealed class InheritedMethodEndpoint : BaseMethodEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/inherited-method";
    }
}

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Ignored
{
    [RequestSizeLimit(1000)]
    [DerivedBodyLimit]
    [DerivedDisableBodyLimit]
    [UnrelatedBodyLimit]
    public sealed class IgnoredGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/ignored");
    }

    [RequestSizeLimit(1100)]
    [DisableRequestSizeLimit]
    [DerivedBodyLimit]
    [DerivedDisableBodyLimit]
    [UnrelatedBodyLimit]
    public sealed class IgnoredEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Fresh
{
    [RequestSizeLimit(1000)]
    [DisableRequestSizeLimit]
    public sealed class FreshGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/fresh");
    }

    [RequestSizeLimit(1300)]
    [DisableRequestSizeLimit]
    public sealed class FreshEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public FreshEndpoint() => throw new InvalidOperationException("Inspection must not construct request targets.");

        [StringSyntax("Route")]
        public string Pattern => "/value";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Scoped
{
    [DisableRequestSizeLimit]
    public sealed class ScopedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/scoped");
    }

    [RequestSizeLimit(1300)]
    public sealed class ScopedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Composed
{
    [DisableRequestSizeLimit]
    public sealed class ParentGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-parent");
    }
}

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Composed.Child
{
    [RequestSizeLimit(1200)]
    public sealed class ChildGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-child");
    }

    public sealed class ChildEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Body
{
    [RequestSizeLimit(1)]
    public sealed class BodyGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/body-group");
    }

    public sealed class BodyGroupEndpoint : IPostEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Task<Ok<ScopeBodyResponse>> HandleAsync(HttpContext context, ScopeBodyProbe probe, CancellationToken cancellationToken) => ScopeBodyReplies.ReadAsync(context, probe, cancellationToken);
    }

    [RequestSizeLimit(1)]
    public sealed class BodyClassEndpoint : IPostEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/body-class";

        public Task<Ok<ScopeBodyResponse>> HandleAsync(HttpContext context, ScopeBodyProbe probe, CancellationToken cancellationToken) => ScopeBodyReplies.ReadAsync(context, probe, cancellationToken);
    }
}

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Reenabled
{
    [DisableRequestSizeLimit(Order = int.MaxValue)]
    public sealed class ReenabledGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/reenabled");
    }

    [RequestSizeLimit(1300, Order = int.MinValue)]
    public sealed class ReenabledEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [DisableRequestSizeLimit(Order = int.MaxValue)]
        public NoContent HandleAsync() => TypedResults.NoContent();

        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithMetadata(new RequestSizeLimitAttribute(1400) { Order = int.MinValue });
    }
}

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Snapshot
{
    [RequestSizeLimit(64)]
    [DisableRequestSizeLimit]
    public sealed class SnapshotEndpoint : IPostEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/snapshot";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Manual
{
    [RequestSizeLimit(1)]
    [ThrowingBodyAuthorization]
    public sealed class ManualThrowingEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public ManualThrowingEndpoint() => throw new InvalidOperationException("Manual targets are excluded from automatic mapping.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/manual";

        public NoContent HandleAsync() => throw new InvalidOperationException("Manual handlers must not execute.");
    }
}

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Disabled
{
    [RequestSizeLimit(1)]
    [ThrowingBodyAuthorization]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled definitions must not map.");
    }

    [RequestSizeLimit(1)]
    [ThrowingBodyAuthorization]
    public sealed class DisabledEndpoint : IGetEndpoint
    {
        public DisabledEndpoint() => throw new InvalidOperationException("Disabled targets must not be constructed.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled";

        public NoContent HandleAsync() => throw new InvalidOperationException("Disabled handlers must not execute.");
    }

    [RequestSizeLimit(1400)]
    public sealed class DisabledAliasEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/disabled-alias";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.IntegrationTest.ScopeFixtures.Disabled.Child
{
    [DisableRequestSizeLimit]
    [ThrowingBodyAuthorization]
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("Disabled ancestors must suppress child conditions.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled children must not map.");
    }

    [RequestSizeLimit(1)]
    [ThrowingBodyAuthorization]
    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        public DisabledChildEndpoint() => throw new InvalidOperationException("Disabled child targets must not be constructed.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-child";

        public NoContent HandleAsync() => throw new InvalidOperationException("Disabled child handlers must not execute.");
    }
}