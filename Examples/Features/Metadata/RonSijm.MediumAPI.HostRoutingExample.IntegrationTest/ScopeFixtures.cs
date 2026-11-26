using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedHostAttribute : Attribute, IHostMetadata
    {
        public UnrelatedHostAttribute() => throw new InvalidOperationException("The selective metadata reader must not construct unrelated host attributes.");
        public IReadOnlyList<string> Hosts => throw new InvalidOperationException("Unselected host getters must not run.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ThrowingAuthorizationAttribute : Attribute, IAuthorizeData
    {
        public ThrowingAuthorizationAttribute() => throw new InvalidOperationException("Skipped metadata must not be constructed.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }

    public sealed class ScopeHostResult : IResult, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) => builder.Metadata.Add(new HostAttribute("provider.scope.test"));
        public Task ExecuteAsync(HttpContext context) => TypedResults.Ok(new HostResponse("ordered")).ExecuteAsync(context);
    }
}

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Ordered
{
    [Host("definition.scope.test")]
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/scope/ordered").RequireHost("map.scope.test");
            ((IEndpointConventionBuilder)group).Finally(builder => builder.Metadata.Add(new HostAttribute("group-final.scope.test")));
            return group;
        }
    }

    [Host("class.scope.test")]
    public sealed class OrderedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [Host("method.scope.test")]
        public ScopeHostResult HandleAsync() => new();

        public static void Configure(RouteHandlerBuilder endpoint)
        {
            endpoint.RequireHost("configured.scope.test");
            endpoint.Finally(builder => builder.Metadata.Add(new HostAttribute("endpoint-final.scope.test")));
        }
    }
}

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Inheritance
{
    [Host("base-group.scope.test")]
    public abstract class GroupBase { }

    public sealed class BaseGroupIgnoredDefinition : GroupBase, INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/base-group");
    }

    public sealed class BaseGroupValueEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("base-group-ignored"));
    }

    [Host("base-class.scope.test")]
    public abstract class BaseClassEndpoint
    {
        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("base-class-ignored"));
    }

    public sealed class BaseClassIgnoredEndpoint : BaseClassEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/base-class-ignored";
    }

    [Host("base-class.scope.test")]
    public abstract class BaseMethodEndpoint
    {
        [Host(SampleHosts.Method)]
        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("inherited-method"));
    }

    [Host(SampleHosts.Api)]
    public sealed class InheritedMethodEndpoint : BaseMethodEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/inherited-method";
    }
}

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Ignored
{
    [Host(SampleHosts.Api)]
    [UnrelatedHost]
    public sealed class IgnoredGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/ignored");
    }

    [UnrelatedHost]
    public sealed class IgnoredEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("ignored"));
    }
}

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Fresh
{
    [Host(SampleHosts.Api, SampleHosts.Alternate)]
    public sealed class FreshGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/fresh");
    }

    [Host(SampleHosts.Method, SampleHosts.Configured)]
    public sealed class FreshEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("fresh"));
    }
}

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Selected
{
    [Host(SampleHosts.Api)]
    public sealed class ActivationEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public ActivationEndpoint(HostProbe probe)
        {
            probe.Record("activation-construction");
            throw new InvalidOperationException("A host mismatch must be rejected before target construction.");
        }

        [StringSyntax("Route")]
        public string Pattern => "/scope/activation";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("activation"));
    }

    [Host(SampleHosts.Api)]
    public sealed class RequestHostEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/request-host";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("request-host"));
    }
}

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Scoped
{
    [Host(SampleHosts.Group)]
    public sealed class ScopedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/scoped");
    }

    [Host(SampleHosts.Api)]
    public sealed class ScopedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("scoped"));
    }
}

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Composed
{
    [Host(SampleHosts.Group)]
    public sealed class ParentGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-parent");
    }
}

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Composed.Child
{
    [Host(SampleHosts.Child)]
    public sealed class ChildGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-child");
    }

    public sealed class ChildEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("child"));
    }
}

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Empty
{
    [Host()]
    public sealed class EmptyGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/empty-group").RequireHost(SampleHosts.Group);
    }

    public sealed class EmptyGroupEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("empty-group"));
    }

    [Host()]
    public sealed class EmptyClassEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/empty-class";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("empty-class"));
    }
}

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Invalid
{
    [Host("invalid.example.test:not-a-port")]
    public sealed class InvalidGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/invalid-group");
    }

    public sealed class InvalidGroupEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("invalid-group"));
    }

    [Host("invalid.example.test:not-a-port")]
    public sealed class InvalidClassEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/invalid-class";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("invalid-class"));
    }
}

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Manual
{
    [Host(SampleHosts.Api)]
    [ThrowingAuthorization]
    public sealed class ManualThrowingEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/manual";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("manual"));
    }
}

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Disabled
{
    [Host(SampleHosts.Group)]
    [ThrowingAuthorization]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("A disabled group must not map.");
    }

    [Host(SampleHosts.Api)]
    [ThrowingAuthorization]
    public sealed class DisabledEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("disabled"));
    }

    [Host(SampleHosts.Method)]
    public sealed class DisabledAliasEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/disabled-alias";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("disabled-alias"));
    }
}

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.ScopeFixtures.Disabled.Child
{
    [Host(SampleHosts.Child)]
    [ThrowingAuthorization]
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("Disabled parents suppress descendant conditions.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled parents suppress descendant mapping.");
    }

    [Host(SampleHosts.Api)]
    [ThrowingAuthorization]
    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-child";

        public Ok<HostResponse> HandleAsync(HostProbe probe) => TypedResults.Ok(probe.Record("disabled-child"));
    }
}