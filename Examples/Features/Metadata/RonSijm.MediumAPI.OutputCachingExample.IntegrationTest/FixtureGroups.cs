using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RonSijm.MediumAPI.OutputCachingExample.IntegrationTest
{
    public sealed record FixtureResponse(int Sequence);

    public sealed record StageMarker(string Stage);

    public sealed class FixtureProbe
    {
        private int calls;
        public int Calls => Volatile.Read(ref calls);
        public FixtureResponse Record() => new(Interlocked.Increment(ref calls));
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedCachePolicyAttribute : Attribute, IOutputCachePolicy
    {
        public UnrelatedCachePolicyAttribute() => throw new InvalidOperationException("An unrelated attribute must not be constructed.");
        public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }
}

namespace RonSijm.MediumAPI.OutputCachingExample.IntegrationTest.Fixtures.Ordered
{
    [OutputCache(Duration = 240, Tags = new[] { "class" }, VaryByRouteValueNames = new[] { "tenant" })]
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/ordered").WithMetadata(new OutputCacheAttribute { Duration = 120, Tags = ["map"] }, new StageMarker("Map"));
    }

    public sealed class OrderedEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [OutputCache(Duration = 480, Tags = new[] { "method" })]
        public Ok<FixtureResponse> HandleAsync(FixtureProbe probe) => TypedResults.Ok(probe.Record());

        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithMetadata(new StageMarker("Endpoint"));
    }
}

namespace RonSijm.MediumAPI.OutputCachingExample.IntegrationTest.Fixtures.Fluent
{
    [OutputCache]
    public sealed class FluentGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/fluent").CacheOutput(policy => policy.NoCache());
    }

    public sealed class FluentEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<FixtureResponse> HandleAsync(FixtureProbe probe) => TypedResults.Ok(probe.Record());
    }
}

namespace RonSijm.MediumAPI.OutputCachingExample.IntegrationTest.Fixtures.Ignored
{
    [OutputCache(NoStore = true)]
    public abstract class BaseCacheDefinition { }

    [UnrelatedCachePolicy]
    public sealed class IgnoredGroupDefinition : BaseCacheDefinition, INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/ignored");
    }

    public sealed class IgnoredEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<FixtureResponse> HandleAsync(FixtureProbe probe) => TypedResults.Ok(probe.Record());
    }
}

namespace RonSijm.MediumAPI.OutputCachingExample.IntegrationTest.Fixtures.Disabled
{
    [OutputCache]
    [UnrelatedCachePolicy]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("A disabled definition must not map.");
    }
}

namespace RonSijm.MediumAPI.OutputCachingExample.IntegrationTest.Fixtures.Disabled.Child
{
    [OutputCache]
    [UnrelatedCachePolicy]
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("A disabled parent's descendant must not evaluate.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("A disabled descendant must not map.");
    }
}