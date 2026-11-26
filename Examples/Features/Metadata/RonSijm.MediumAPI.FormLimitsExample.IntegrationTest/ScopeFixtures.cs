using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DerivedFormLimitsAttribute : RequestFormLimitsAttribute
    {
        public DerivedFormLimitsAttribute() => throw new InvalidOperationException("Derived form-limit attributes must not be constructed by the selective reader.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedFormOptionsAttribute : Attribute, IFormOptionsMetadata
    {
        public UnrelatedFormOptionsAttribute() => throw new InvalidOperationException("Arbitrary form metadata is not an automatic class convention.");
        public bool? BufferBody => null;
        public int? MemoryBufferThreshold => null;
        public long? BufferBodyLengthLimit => null;
        public int? ValueCountLimit => null;
        public int? KeyLengthLimit => null;
        public int? ValueLengthLimit => null;
        public int? MultipartBoundaryLengthLimit => null;
        public int? MultipartHeadersCountLimit => null;
        public int? MultipartHeadersLengthLimit => null;
        public long? MultipartBodyLengthLimit => null;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ThrowingFormAuthorizationAttribute : Attribute, IAuthorizeData
    {
        public ThrowingFormAuthorizationAttribute() => throw new InvalidOperationException("Skipped supported declarations must not be constructed.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }

    public sealed class ScopeFormResult : IResult, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) => builder.Metadata.Add(new RequestFormLimitsAttribute { ValueCountLimit = 1400 });
        public Task ExecuteAsync(HttpContext context) => TypedResults.NoContent().ExecuteAsync(context);
    }

    public sealed record ScopeFormResponse(int Count, string FirstValue, bool SameInstance);

    public sealed class ScopeFormProbe
    {
        private int calls;
        public int Calls => calls;
        public void Record() => Interlocked.Increment(ref calls);
    }

    internal static class ScopeFormReplies
    {
        internal static async Task<Ok<ScopeFormResponse>> ReplyAsync(IFormCollection form, HttpContext context, ScopeFormProbe probe, CancellationToken cancellationToken)
        {
            var repeated = await context.Request.ReadFormAsync(cancellationToken);
            probe.Record();
            return TypedResults.Ok(new ScopeFormResponse(form.Count, form["first"].ToString(), ReferenceEquals(form, repeated)));
        }
    }
}

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Ordered
{
    [RequestFormLimits(ValueCountLimit = 1100, Order = int.MaxValue)]
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/scope/ordered").WithMetadata(new RequestFormLimitsAttribute { ValueCountLimit = 1000 });
            ((IEndpointConventionBuilder)group).Finally(builder => builder.Metadata.Add(new RequestFormLimitsAttribute { ValueCountLimit = 1800 }));
            return group;
        }
    }

    [RequestFormLimits(ValueCountLimit = 1300, Order = int.MinValue)]
    public sealed class OrderedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [RequestFormLimits(ValueCountLimit = 1500, Order = -1000)]
        public ScopeFormResult HandleAsync() => new();

        public static void Configure(RouteHandlerBuilder endpoint)
        {
            endpoint.WithMetadata(new RequestFormLimitsAttribute { ValueCountLimit = 1600 });
            endpoint.Finally(builder => builder.Metadata.Add(new RequestFormLimitsAttribute { ValueCountLimit = 1700 }));
        }
    }
}

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Inheritance
{
    [RequestFormLimits(ValueCountLimit = 1000)]
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

    [RequestFormLimits(ValueCountLimit = 1000)]
    public abstract class BaseClassEndpoint
    {
        public NoContent HandleAsync() => TypedResults.NoContent();
    }

    public sealed class BaseClassIgnoredEndpoint : BaseClassEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/base-class-ignored";
    }

    [RequestFormLimits(ValueCountLimit = 1000)]
    public abstract class BaseMethodEndpoint
    {
        [RequestFormLimits(ValueCountLimit = 1500)]
        public NoContent HandleAsync() => TypedResults.NoContent();
    }

    [RequestFormLimits(ValueCountLimit = 1300)]
    public sealed class InheritedMethodEndpoint : BaseMethodEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/inherited-method";
    }
}

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Ignored
{
    [RequestFormLimits(ValueCountLimit = 1000)]
    [DerivedFormLimits]
    [UnrelatedFormOptions]
    public sealed class IgnoredGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/ignored");
    }

    [RequestFormLimits(ValueCountLimit = 1100)]
    [DerivedFormLimits]
    [UnrelatedFormOptions]
    public sealed class IgnoredEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Fresh
{
    [RequestFormLimits(ValueCountLimit = 1000)]
    public sealed class FreshGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/fresh");
    }

    [RequestFormLimits(ValueCountLimit = 1300)]
    public sealed class FreshEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public FreshEndpoint() => throw new InvalidOperationException("Inspection must not construct request targets.");

        [StringSyntax("Route")]
        public string Pattern => "/value";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Scoped
{
    [RequestFormLimits(ValueCountLimit = 1000)]
    public sealed class ScopedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/scoped");
    }

    [RequestFormLimits(ValueCountLimit = 1300)]
    public sealed class ScopedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Composed
{
    [RequestFormLimits(ValueCountLimit = 1000)]
    public sealed class ParentGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-parent");
    }
}

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Composed.Child
{
    [RequestFormLimits(ValueCountLimit = 1200)]
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

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Forms
{
    [RequestFormLimits(ValueCountLimit = 1)]
    public sealed class BoundFormEndpoint : IPostEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/form";

        public Task<Ok<ScopeFormResponse>> HandleAsync(IFormCollection form, HttpContext context, ScopeFormProbe probe, CancellationToken cancellationToken) => ScopeFormReplies.ReplyAsync(form, context, probe, cancellationToken);
        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.DisableAntiforgery();
    }

    [RequestFormLimits(ValueCountLimit = 2)]
    public sealed class ResetFormEndpoint : IPostEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/reset";

        public Task<Ok<ScopeFormResponse>> HandleAsync(IFormCollection form, HttpContext context, ScopeFormProbe probe, CancellationToken cancellationToken) => ScopeFormReplies.ReplyAsync(form, context, probe, cancellationToken);
        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.DisableAntiforgery();
    }

    [RequestFormLimits(ValueCountLimit = 1)]
    public sealed class ValidatedFormEndpoint : IPostEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/validated-form";

        public Task<Ok<ScopeFormResponse>> HandleAsync(IFormCollection form, HttpContext context, ScopeFormProbe probe, CancellationToken cancellationToken) => ScopeFormReplies.ReplyAsync(form, context, probe, cancellationToken);
    }
}

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Snapshot
{
    [RequestFormLimits(ValueCountLimit = 64, ValueLengthLimit = 32)]
    public sealed class SnapshotEndpoint : IPostEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/snapshot";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Manual
{
    [RequestFormLimits(ValueCountLimit = 1)]
    [ThrowingFormAuthorization]
    public sealed class ManualThrowingEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public ManualThrowingEndpoint() => throw new InvalidOperationException("Manual targets are excluded from automatic mapping.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/manual";

        public NoContent HandleAsync() => throw new InvalidOperationException("Manual handlers must not execute.");
    }
}

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Disabled
{
    [RequestFormLimits(ValueCountLimit = 1)]
    [ThrowingFormAuthorization]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled definitions must not map.");
    }

    [RequestFormLimits(ValueCountLimit = 1)]
    [ThrowingFormAuthorization]
    public sealed class DisabledEndpoint : IGetEndpoint
    {
        public DisabledEndpoint() => throw new InvalidOperationException("Disabled targets must not be constructed.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled";

        public NoContent HandleAsync() => throw new InvalidOperationException("Disabled handlers must not execute.");
    }

    [RequestFormLimits(ValueCountLimit = 1400)]
    public sealed class DisabledAliasEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/disabled-alias";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Disabled.Child
{
    [RequestFormLimits(ValueCountLimit = 1)]
    [ThrowingFormAuthorization]
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("Disabled ancestors must suppress child conditions.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled children must not map.");
    }

    [RequestFormLimits(ValueCountLimit = 1)]
    [ThrowingFormAuthorization]
    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        public DisabledChildEndpoint() => throw new InvalidOperationException("Disabled child targets must not be constructed.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-child";

        public NoContent HandleAsync() => throw new InvalidOperationException("Disabled child handlers must not execute.");
    }
}