using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest
{
    public sealed record ScopeReceipt(string Kind, int Sequence);

    public sealed class ScopeProbe
    {
        private readonly ConcurrentDictionary<string, int> calls = new(StringComparer.Ordinal);
        private int constructions;
        private int disposals;
        public int Constructions => Volatile.Read(ref constructions);
        public int Disposals => Volatile.Read(ref disposals);
        public int CallsFor(string kind) => calls.GetValueOrDefault(kind);
        public ScopeReceipt Record(string kind) => new(kind, calls.AddOrUpdate(kind, 1, (_, count) => count + 1));
        public void Constructed() => Interlocked.Increment(ref constructions);
        public void Disposed() => Interlocked.Increment(ref disposals);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedCachePolicyAttribute : Attribute, IOutputCachePolicy
    {
        public UnrelatedCachePolicyAttribute() => throw new InvalidOperationException("Unrelated cache-policy attributes must not be constructed.");
        public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken) => throw new InvalidOperationException("Unselected cache policies must not run.");
        public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) => throw new InvalidOperationException("Unselected cache policies must not run.");
        public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken) => throw new InvalidOperationException("Unselected cache policies must not run.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedMvcAttribute : Attribute, IAsyncActionFilter
    {
        public UnrelatedMvcAttribute() => throw new InvalidOperationException("Unrelated MVC attributes must not be constructed.");
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) => throw new InvalidOperationException("MVC action filters do not execute through this feature.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ThrowingAuthorizationAttribute : Attribute, IAuthorizeData
    {
        public ThrowingAuthorizationAttribute() => throw new InvalidOperationException("Suppressed endpoint metadata must not be constructed.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }

    public sealed class ScopeMetadataResult : IResult, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) => builder.Metadata.Add(new OutputCacheAttribute { Duration = 360, Tags = ["provider"] });
        public Task ExecuteAsync(HttpContext context) => TypedResults.Ok(context.RequestServices.GetRequiredService<ScopeProbe>().Record("ordered")).ExecuteAsync(context);
    }
}

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Ordered
{
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/ordered").WithMetadata(new OutputCacheAttribute { Duration = 120, Tags = ["group"] });
    }

    [OutputCache(Duration = 240, Tags = new[] { "class" }, VaryByRouteValueNames = new[] { "tenant" })]
    public sealed class OrderedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [OutputCache(Duration = 480, Tags = new[] { "method" })]
        public ScopeMetadataResult HandleAsync() => new();

        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithMetadata(new OutputCacheAttribute { Duration = 600, Tags = ["configure"] });
    }
}

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Inheritance
{
    [OutputCache(NoStore = true)]
    public abstract class BaseClassEndpoint
    {
        public Ok<ScopeReceipt> HandleAsync(ScopeProbe probe) => TypedResults.Ok(probe.Record("base-class-ignored"));
    }

    public sealed class BaseClassIgnoredEndpoint : BaseClassEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/base-class-ignored";
    }

    [OutputCache(Duration = 120)]
    public abstract class BaseMethodEndpoint
    {
        [OutputCache(NoStore = true)]
        public Ok<ScopeReceipt> HandleAsync(ScopeProbe probe) => TypedResults.Ok(probe.Record("inherited-method"));
    }

    [OutputCache(Duration = 600)]
    public sealed class InheritedMethodEndpoint : BaseMethodEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/inherited-method";
    }
}

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Selected
{
    [OutputCache]
    [UnrelatedCachePolicy]
    [UnrelatedMvc]
    public sealed class IgnoredAttributesEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/ignored-attributes";

        public Ok<ScopeReceipt> HandleAsync(ScopeProbe probe) => TypedResults.Ok(probe.Record("ignored-attributes"));
    }

    [OutputCache(Duration = 600, Tags = new[] { "scope" }, VaryByQueryKeys = new[] { "category" }, VaryByHeaderNames = new[] { "X-Scope-Language" }, VaryByRouteValueNames = new[] { "tenant" })]
    public sealed class FreshAttributesEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/fresh";

        public Ok<ScopeReceipt> HandleAsync(ScopeProbe probe) => TypedResults.Ok(probe.Record("fresh"));
    }

    [OutputCache]
    public sealed class DefaultDurationEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/default";

        public Ok<ScopeReceipt> HandleAsync(ScopeProbe probe) => TypedResults.Ok(probe.Record("default"));
    }

    [OutputCache(Duration = 0)]
    public sealed class ZeroDurationEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/zero";

        public Ok<ScopeReceipt> HandleAsync(ScopeProbe probe) => TypedResults.Ok(probe.Record("zero"));
    }

    [OutputCache(Duration = 600)]
    public sealed class ActivationEndpoint : IGetEndpoint, IManuallyMappedEndpoint, IDisposable
    {
        private readonly ScopeProbe probe;

        public ActivationEndpoint(ScopeProbe probe)
        {
            this.probe = probe;
            probe.Constructed();
        }

        [StringSyntax("Route")]
        public string Pattern => "/scope/activation";

        public Ok<ScopeReceipt> HandleAsync() => TypedResults.Ok(probe.Record("activation"));
        public void Dispose() => probe.Disposed();
    }

    [OutputCache(PolicyName = "NotRegistered")]
    public sealed class MissingPolicyEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/missing-policy";

        public Ok<ScopeReceipt> HandleAsync(ScopeProbe probe) => TypedResults.Ok(probe.Record("missing-policy"));
    }

    [Authorize]
    [OutputCache]
    public sealed class MixedAuthorizationEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/mixed";

        public Ok<ScopeReceipt> HandleAsync(ScopeProbe probe) => TypedResults.Ok(probe.Record("mixed"));
    }
}

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Scoped
{
    public sealed class ScopedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/scoped").WithMetadata(new OutputCacheAttribute { NoStore = true });
    }

    [OutputCache(Duration = 600)]
    public sealed class ScopedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<ScopeReceipt> HandleAsync(ScopeProbe probe) => TypedResults.Ok(probe.Record("scoped"));
    }
}

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Disabled
{
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled definitions must not map.");
    }

    [OutputCache]
    [ThrowingAuthorization]
    public sealed class DisabledEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled";

        public Ok<ScopeReceipt> HandleAsync(ScopeProbe probe) => TypedResults.Ok(probe.Record("disabled"));
    }

    [OutputCache]
    public sealed class DisabledAliasEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-alias";

        public Ok<ScopeReceipt> HandleAsync(ScopeProbe probe) => TypedResults.Ok(probe.Record("disabled-alias"));
    }
}

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Disabled.Child
{
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("A disabled parent's descendant must not evaluate.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled descendants must not map.");
    }

    [OutputCache]
    [ThrowingAuthorization]
    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-child";

        public Ok<ScopeReceipt> HandleAsync(ScopeProbe probe) => TypedResults.Ok(probe.Record("disabled-child"));
    }
}

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.IntegrationTest.ScopeFixtures.Manual
{
    [OutputCache]
    [ThrowingAuthorization]
    public sealed class ManualThrowingEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/manual";

        public Ok<ScopeReceipt> HandleAsync(ScopeProbe probe) => TypedResults.Ok(probe.Record("manual"));
    }
}