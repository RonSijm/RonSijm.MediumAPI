using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest
{
    public sealed record ScopeReply(string Value);

    public sealed class ScopeProbe
    {
        private int constructions;
        private int resolutions;
        private int disposals;
        public int Constructions => Volatile.Read(ref constructions);
        public int Resolutions => Volatile.Read(ref resolutions);
        public int Disposals => Volatile.Read(ref disposals);
        public void Construct() => Interlocked.Increment(ref constructions);
        public void Resolve() => Interlocked.Increment(ref resolutions);
        public void DisposeTarget() => Interlocked.Increment(ref disposals);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedScopeAttribute : Attribute
    {
        public UnrelatedScopeAttribute() => throw new InvalidOperationException("Unrelated attributes must not be constructed during activation inspection.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ThrowingScopeAuthorizationAttribute : Attribute, IAuthorizeData
    {
        public ThrowingScopeAuthorizationAttribute() => throw new InvalidOperationException("Skipped declarations must not be constructed.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }

    public sealed class ScopeFactoryFailureException(string message) : Exception(message);

    public sealed class ScopeResultFailureException(string message) : Exception(message);

    public sealed class ScopeFailureResult : IResult
    {
        public static readonly ScopeResultFailureException Failure = new("The synthetic result failed.");
        public Task ExecuteAsync(HttpContext context) => throw Failure;
    }
}

namespace RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ScopeFixtures.Basic
{
    [EndpointFromServices]
    [UnrelatedScope]
    public sealed class LazyEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        private readonly string value;
        public LazyEndpoint() => value = "constructed";
        internal LazyEndpoint(string value) => this.value = value;

        [StringSyntax("Route")]
        public string Pattern => "/scope/lazy";

        public Ok<ScopeReply> HandleAsync() => TypedResults.Ok(new ScopeReply(value));
    }

    [EndpointFromServices]
    public abstract class MarkedBase : IGetEndpoint, IManuallyMappedEndpoint
    {
        protected MarkedBase(string value) => Value = value;
        protected string Value { get; }

        [StringSyntax("Route")]
        public abstract string Pattern { get; }

        [EndpointResponse(StatusCodes.Status418ImATeapot)]
        public Ok<ScopeReply> HandleAsync() => TypedResults.Ok(new ScopeReply(Value));
    }

    public sealed class UnmarkedInheritedEndpoint : MarkedBase
    {
        public UnmarkedInheritedEndpoint() : base("constructed") { }
        internal UnmarkedInheritedEndpoint(string value) : base(value) { }

        [StringSyntax("Route")]
        public override string Pattern => "/scope/inherited/default";
    }

    [EndpointFromServices]
    public sealed class MarkedInheritedEndpoint : MarkedBase
    {
        public MarkedInheritedEndpoint() : base("constructed") { }
        internal MarkedInheritedEndpoint(string value) : base(value) { }

        [StringSyntax("Route")]
        public override string Pattern => "/scope/inherited/service";
    }

    [EndpointFromServices]
    public sealed class MissingEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public MissingEndpoint(ScopeProbe probe) => probe.Construct();

        [StringSyntax("Route")]
        public string Pattern => "/scope/missing";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }

    [EndpointFromServices]
    public sealed class NullEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public NullEndpoint(ScopeProbe probe) => probe.Construct();

        [StringSyntax("Route")]
        public string Pattern => "/scope/null";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }

    [EndpointFromServices]
    public sealed class ThrowingFactoryEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public ThrowingFactoryEndpoint(ScopeProbe probe) => probe.Construct();

        [StringSyntax("Route")]
        public string Pattern => "/scope/factory-error";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }

    [EndpointFromServices]
    public sealed class ProviderEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        private readonly string value;
        public ProviderEndpoint() => value = "constructed";
        internal ProviderEndpoint(string value) => this.value = value;

        [StringSyntax("Route")]
        public string Pattern => "/scope/provider";

        public Ok<ScopeReply> HandleAsync() => TypedResults.Ok(new ScopeReply(value));
    }

    [EndpointFromServices]
    public class VirtualEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public VirtualEndpoint() { }

        [StringSyntax("Route")]
        public string Pattern => "/scope/virtual";

        public virtual Ok<ScopeReply> HandleAsync() => TypedResults.Ok(new ScopeReply("base"));
    }

    public sealed class DerivedFactoryEndpoint : VirtualEndpoint
    {
        public override Ok<ScopeReply> HandleAsync() => TypedResults.Ok(new ScopeReply("derived-factory"));
    }

    [EndpointFromServices]
    public sealed class ContractEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/contract";

        public Ok<ScopeReply> HandleAsync() => TypedResults.Ok(new ScopeReply("contract"));
    }

    [EndpointFromServices]
    public sealed class ThrowingResultEndpoint : IGetEndpoint, IManuallyMappedEndpoint, IAsyncDisposable
    {
        private readonly ScopeProbe probe;

        public ThrowingResultEndpoint(ScopeProbe probe)
        {
            this.probe = probe;
            probe.Construct();
        }

        [StringSyntax("Route")]
        public string Pattern => "/scope/result-error";

        public ScopeFailureResult HandleAsync() => new();

        public ValueTask DisposeAsync()
        {
            probe.DisposeTarget();
            return ValueTask.CompletedTask;
        }
    }
}

namespace RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ScopeFixtures.Scoped
{
    [Host("service.scope.test")]
    public sealed class ScopedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/grouped");
    }

    [EndpointFromServices]
    public sealed class AliasEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        private readonly string value;
        public AliasEndpoint() => value = "constructed";
        internal AliasEndpoint(string value) => this.value = value;

        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<ScopeReply> HandleAsync() => TypedResults.Ok(new ScopeReply(value));
    }
}

namespace RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ScopeFixtures.Parent
{
    [Host("ancestor.scope.test")]
    [EndpointFromServices]
    public sealed class ParentGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-parent");
    }
}

namespace RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ScopeFixtures.Parent.Child
{
    public sealed class ChildGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-child");
    }

    [EndpointFromServices]
    public sealed class ChildEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        private readonly string value;
        public ChildEndpoint() => value = "constructed";
        internal ChildEndpoint(string value) => this.value = value;

        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<ScopeReply> HandleAsync() => TypedResults.Ok(new ScopeReply(value));
    }
}

namespace RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ScopeFixtures.GroupOnly
{
    [EndpointFromServices]
    public sealed class MarkerGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/group-marker");
    }

    public sealed class UnmarkedChildEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        private readonly string value;
        public UnmarkedChildEndpoint() => value = "constructed";
        internal UnmarkedChildEndpoint(string value) => this.value = value;

        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<ScopeReply> HandleAsync() => TypedResults.Ok(new ScopeReply(value));
    }
}

namespace RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ScopeFixtures.Disabled
{
    [ThrowingScopeAuthorization]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled namespaces must not map.");
    }

    [EndpointFromServices]
    [ThrowingScopeAuthorization]
    public sealed class DisabledEndpoint : IGetEndpoint
    {
        public DisabledEndpoint() => throw new InvalidOperationException("Disabled endpoints must not be constructed.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled";

        public NoContent HandleAsync() => throw new InvalidOperationException("Disabled handlers must not execute.");
    }
}

namespace RonSijm.MediumAPI.ServiceEndpointsExample.IntegrationTest.ScopeFixtures.Disabled.Child
{
    [ThrowingScopeAuthorization]
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("Disabled ancestors must suppress child conditions.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled child namespaces must not map.");
    }

    [EndpointFromServices]
    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        public DisabledChildEndpoint() => throw new InvalidOperationException("Disabled child targets must not be constructed.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-child";

        public NoContent HandleAsync() => throw new InvalidOperationException("Disabled child handlers must not execute.");
    }
}