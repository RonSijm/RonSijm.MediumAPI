using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedTimeoutAttribute : Attribute
    {
        public UnrelatedTimeoutAttribute() => throw new InvalidOperationException("The selective reader must not construct unrelated timeout attributes.");
        public RequestTimeoutPolicy Policy => throw new InvalidOperationException("Unselected policy getters must not run.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ThrowingAuthorizationAttribute : Attribute, IAuthorizeData
    {
        public ThrowingAuthorizationAttribute() => throw new InvalidOperationException("Skipped supported metadata must not be constructed.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }

    public sealed class ScopeTimeoutResult : IResult, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) => builder.Metadata.Add(new RequestTimeoutAttribute(14000));
        public Task ExecuteAsync(HttpContext context) => TypedResults.NoContent().ExecuteAsync(context);
    }

    internal static class ScopeReplies
    {
        internal static Ok<TimeoutObservation> Reply(HttpContext context, TimeoutProbe probe, string value)
        {
            probe.Record(value);
            return TypedResults.Ok(new TimeoutObservation(value, context.Features.Get<IHttpRequestTimeoutFeature>() is not null));
        }
    }
}

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Ordered
{
    [RequestTimeout(11000)]
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/scope/ordered").WithMetadata(new RequestTimeoutAttribute(10000));
            ((IEndpointConventionBuilder)group).Finally(builder => builder.Metadata.Add(new RequestTimeoutAttribute(18000)));
            return group;
        }
    }

    [RequestTimeout(13000)]
    public sealed class OrderedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [RequestTimeout(15000)]
        public ScopeTimeoutResult HandleAsync() => new();

        public static void Configure(RouteHandlerBuilder endpoint)
        {
            endpoint.WithMetadata(new RequestTimeoutAttribute(16000));
            endpoint.Finally(builder => builder.Metadata.Add(new RequestTimeoutAttribute(17000)));
        }
    }
}

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Inheritance
{
    [RequestTimeout(10000)]
    [DisableRequestTimeout]
    public abstract class GroupBase { }

    public sealed class BaseGroupIgnoredDefinition : GroupBase, INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/base-group");
    }

    public sealed class BaseGroupValueEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<TimeoutObservation> HandleAsync(HttpContext context, TimeoutProbe probe) => ScopeReplies.Reply(context, probe, "base-group-ignored");
    }

    [RequestTimeout(10000)]
    [DisableRequestTimeout]
    public abstract class BaseClassEndpoint
    {
        public Ok<TimeoutObservation> HandleAsync(HttpContext context, TimeoutProbe probe) => ScopeReplies.Reply(context, probe, "base-class-ignored");
    }

    public sealed class BaseClassIgnoredEndpoint : BaseClassEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/base-class-ignored";
    }

    [DisableRequestTimeout]
    public abstract class BaseMethodEndpoint
    {
        [RequestTimeout(15000)]
        public Ok<TimeoutObservation> HandleAsync(HttpContext context, TimeoutProbe probe) => ScopeReplies.Reply(context, probe, "inherited-method");
    }

    [RequestTimeout(13000)]
    public sealed class InheritedMethodEndpoint : BaseMethodEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/inherited-method";
    }
}

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Ignored
{
    [RequestTimeout(10000)]
    [UnrelatedTimeout]
    public sealed class IgnoredGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/ignored");
    }

    [RequestTimeout(11000)]
    [DisableRequestTimeout]
    [UnrelatedTimeout]
    public sealed class IgnoredEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<TimeoutObservation> HandleAsync(HttpContext context, TimeoutProbe probe) => ScopeReplies.Reply(context, probe, "ignored");
    }
}

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Fresh
{
    [RequestTimeout(10000)]
    [DisableRequestTimeout]
    public sealed class FreshGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/fresh");
    }

    [RequestTimeout(13000)]
    [DisableRequestTimeout]
    public sealed class FreshEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<TimeoutObservation> HandleAsync(HttpContext context, TimeoutProbe probe) => ScopeReplies.Reply(context, probe, "fresh");
    }
}

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Scoped
{
    [DisableRequestTimeout]
    public sealed class ScopedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/scoped");
    }

    [RequestTimeout(13000)]
    public sealed class ScopedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<TimeoutObservation> HandleAsync(HttpContext context, TimeoutProbe probe) => ScopeReplies.Reply(context, probe, "scoped");
    }
}

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Composed
{
    [DisableRequestTimeout]
    public sealed class ParentGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-parent");
    }
}

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Composed.Child
{
    [RequestTimeout(12000)]
    public sealed class ChildGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-child");
    }

    public sealed class ChildEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<TimeoutObservation> HandleAsync(HttpContext context, TimeoutProbe probe) => ScopeReplies.Reply(context, probe, "child");
    }
}

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Missing
{
    [RequestTimeout(TimeoutPolicies.Missing)]
    public sealed class MissingGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope");
    }

    public sealed class MissingGroupEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/missing";

        public Ok<TimeoutObservation> HandleAsync(HttpContext context, TimeoutProbe probe) => ScopeReplies.Reply(context, probe, "missing-group");
    }

    [RequestTimeout(TimeoutPolicies.Missing)]
    public sealed class MissingClassEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/missing";

        public Ok<TimeoutObservation> HandleAsync(HttpContext context, TimeoutProbe probe) => ScopeReplies.Reply(context, probe, "missing-class");
    }
}

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Ownership
{
    [RequestTimeout(250)]
    public sealed class OwnedTimeoutEndpoint : IGetEndpoint, IManuallyMappedEndpoint, IDisposable
    {
        private readonly TimeoutProbe probe;

        public OwnedTimeoutEndpoint(TimeoutProbe probe)
        {
            this.probe = probe;
            probe.Record("owned-construction");
        }

        [StringSyntax("Route")]
        public string Pattern => "/scope/owned-timeout";

        public async Task<NoContent> HandleAsync(CancellationToken cancellationToken)
        {
            probe.Record("owned-handler");
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return TypedResults.NoContent();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                probe.RecordCancellation();
                throw;
            }
        }

        public void Dispose() => probe.Record("owned-disposal");
    }
}

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Manual
{
    [RequestTimeout(10000)]
    [ThrowingAuthorization]
    public sealed class ManualThrowingEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/manual";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Disabled
{
    [RequestTimeout(10000)]
    [DisableRequestTimeout]
    [ThrowingAuthorization]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("A disabled group must not map.");
    }

    [RequestTimeout(11000)]
    [ThrowingAuthorization]
    public sealed class DisabledEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }

    [RequestTimeout(14000)]
    public sealed class DisabledAliasEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/disabled-alias";

        public Ok<TimeoutObservation> HandleAsync(HttpContext context, TimeoutProbe probe) => ScopeReplies.Reply(context, probe, "disabled-alias");
    }
}

namespace RonSijm.MediumAPI.RequestTimeoutsExample.IntegrationTest.ScopeFixtures.Disabled.Child
{
    [RequestTimeout(12000)]
    [ThrowingAuthorization]
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("Disabled parents suppress descendant conditions.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled parents suppress descendant mapping.");
    }

    [RequestTimeout(13000)]
    [ThrowingAuthorization]
    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-child";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}