using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest
{
    public sealed record FixtureReceipt(string Handler, int Sequence, bool? ValidationSucceeded);

    public sealed record StageMarker(string Stage);

    public sealed class FixtureProbe
    {
        private readonly ConcurrentDictionary<string, int> counts = new(StringComparer.Ordinal);
        public int CallsFor(string handler) => counts.GetValueOrDefault(handler);
        public FixtureReceipt Record(string handler, bool? validationSucceeded = null) => new(handler, counts.AddOrUpdate(handler, 1, (_, count) => count + 1), validationSucceeded);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DerivedAntiforgeryAttribute : RequireAntiforgeryTokenAttribute
    {
        public DerivedAntiforgeryAttribute() => throw new InvalidOperationException("A derived attribute must not be constructed.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedAntiforgeryAttribute : Attribute, IAntiforgeryMetadata
    {
        public UnrelatedAntiforgeryAttribute() => throw new InvalidOperationException("An unrelated attribute must not be constructed.");
        public bool RequiresValidation => true;
    }
}

namespace RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.Fixtures.Ordered
{
    [RequireAntiforgeryToken(false)]
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/ordered").WithMetadata(new RequireAntiforgeryTokenAttribute(true), new StageMarker("Map"));
    }

    public sealed class OrderedEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [RequireAntiforgeryToken(true)]
        public Ok<FixtureReceipt> HandleAsync(FixtureProbe probe) => TypedResults.Ok(probe.Record("ordered"));

        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithMetadata(new RequireAntiforgeryTokenAttribute(false), new StageMarker("Endpoint"));
    }
}

namespace RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.Fixtures.Ignored
{
    [RequireAntiforgeryToken]
    public abstract class AntiforgeryBaseDefinition { }

    [DerivedAntiforgery]
    [UnrelatedAntiforgery]
    public sealed class IgnoredGroupDefinition : AntiforgeryBaseDefinition, INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/ignored");
    }

    public sealed class IgnoredEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<FixtureReceipt> HandleAsync(FixtureProbe probe) => TypedResults.Ok(probe.Record("ignored"));
    }
}

namespace RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.Fixtures.MetadataOnly
{
    [RequireAntiforgeryToken]
    public sealed class MetadataOnlyGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/metadata-only");
    }

    public sealed class PostMetadataOnlyEndpoint : IPostEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<FixtureReceipt> HandleAsync(HttpContext context, FixtureProbe probe) => TypedResults.Ok(probe.Record("metadata-only", context.Features.Get<IAntiforgeryValidationFeature>()?.IsValid));
    }

    public sealed class GetMetadataOnlyEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/safe";

        public Ok<FixtureReceipt> HandleAsync(HttpContext context, FixtureProbe probe) => TypedResults.Ok(probe.Record("safe", context.Features.Get<IAntiforgeryValidationFeature>()?.IsValid));
    }
}

namespace RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.Fixtures.Disabled
{
    [RequireAntiforgeryToken]
    [DerivedAntiforgery]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("A disabled definition must not map.");
    }
}

namespace RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.Fixtures.Disabled.Child
{
    [RequireAntiforgeryToken]
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("A disabled parent's descendant must not evaluate.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("A disabled descendant must not map.");
    }

    public sealed class DisabledEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<FixtureReceipt> HandleAsync(FixtureProbe probe) => TypedResults.Ok(probe.Record("disabled"));
    }
}