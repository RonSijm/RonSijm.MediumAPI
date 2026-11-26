using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest
{
    public sealed record ScopePayload(string Value);

    public sealed record ScopeError(string Reason);

    public sealed record ScopeConfigureMarker;

    public sealed class ScopeProbe
    {
        private int handlerCalls;
        private int filterCalls;
        public int HandlerCalls => Volatile.Read(ref handlerCalls);
        public int FilterCalls => Volatile.Read(ref filterCalls);
        public void Handle() => Interlocked.Increment(ref handlerCalls);
        public void Filter() => Interlocked.Increment(ref filterCalls);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedResponseAttribute : Attribute, IProducesResponseTypeMetadata
    {
        public UnrelatedResponseAttribute() => throw new InvalidOperationException("Unrelated response attributes must not be constructed by selective class readers.");
        public int StatusCode => 499;
        public Type? Type => typeof(void);
        public IEnumerable<string> ContentTypes => [];
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ThrowingScopeAuthorizationAttribute : Attribute, IAuthorizeData
    {
        public ThrowingScopeAuthorizationAttribute() => throw new InvalidOperationException("Skipped supported declarations must not be constructed.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }

    public sealed class ScopeProviderResult : IResult, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) => builder.Metadata.Add(new EndpointResponseAttribute(414));
        public Task ExecuteAsync(HttpContext context) => TypedResults.NoContent().ExecuteAsync(context);
    }

    [EndpointResponse(417)]
    [ResponseHeader("X-Scope-Declared", "declared")]
    public sealed class OrderedResponseFilter : IMediumApiEndpointFilter
    {
        public static void Configure(IEndpointConventionBuilder endpoint) => endpoint.WithMetadata(new EndpointResponseAttribute(416)).WithResponseHeader("X-Scope-Configured", "configured");
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) => next(context);
    }

    [EndpointResponse(487)]
    [UnrelatedResponse]
    public sealed class PlainResponseFilter : IEndpointFilter
    {
        public static void Configure(IEndpointConventionBuilder endpoint) => throw new InvalidOperationException("Ordinary native filters do not opt into the static convention contract.");

        [EndpointResponse(488)]
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            context.HttpContext.RequestServices.GetRequiredService<ScopeProbe>().Filter();
            return next(context);
        }
    }

    [EndpointResponse(490)]
    [ResponseHeader("X-Scope-Repeated", "present")]
    public sealed class RepeatedResponseFilter : IMediumApiEndpointFilter
    {
        public static void Configure(IEndpointConventionBuilder endpoint) => endpoint.WithMetadata(new ScopeConfigureMarker(), new EndpointResponseAttribute(489));

        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            context.HttpContext.RequestServices.GetRequiredService<ScopeProbe>().Filter();
            return next(context);
        }
    }

    [EndpointResponse(491, typeof(string), "text/plain", null!)]
    public sealed class MalformedResponseFilter : IMediumApiEndpointFilter
    {
        public static void Configure(IEndpointConventionBuilder endpoint) => endpoint.WithMetadata(new ScopeConfigureMarker());
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) => next(context);
    }

    [EndpointResponse(491, typeof(string), "text/plain", null!)]
    public sealed class ThrowingConfigureFilter : IMediumApiEndpointFilter
    {
        public static readonly InvalidOperationException Failure = new("Explicit filter configuration failed.");
        public static void Configure(IEndpointConventionBuilder endpoint) => throw Failure;
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) => next(context);
    }

    [EndpointResponse(440)]
    public abstract class ResponseFilterBase : IEndpointFilter
    {
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) => next(context);
    }

    [EndpointResponse(441)]
    public sealed class DerivedResponseFilter : ResponseFilterBase { }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Ordered
{
    [EndpointResponse(411)]
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/scope/ordered").WithMetadata(new EndpointResponseAttribute(410));
            ((IEndpointConventionBuilder)group).Finally(builder => builder.Metadata.Add(new EndpointResponseAttribute(420)));
            return group;
        }
    }

    [EndpointResponse(413)]
    [EndpointFilter<OrderedResponseFilter>]
    public sealed class OrderedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [EndpointResponse(415)]
        public ScopeProviderResult HandleAsync() => new();

        public static void Configure(RouteHandlerBuilder endpoint)
        {
            endpoint.WithMetadata(new EndpointResponseAttribute(418));
            endpoint.Finally(builder => builder.Metadata.Add(new EndpointResponseAttribute(419)));
        }
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Inheritance
{
    [EndpointResponse(440)]
    public abstract class GroupBase { }

    public sealed class InheritedGroupDefinition : GroupBase, INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/inherited");
    }

    [EndpointResponse(440)]
    public abstract class EndpointBase
    {
        [EndpointResponse(442)]
        public Ok<ScopePayload> HandleAsync() => TypedResults.Ok(new ScopePayload("inherited"));
    }

    [EndpointResponse(441)]
    [EndpointFilter<DerivedResponseFilter>]
    public sealed class InheritedEndpoint : EndpointBase, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Formats
{
    [EndpointResponse(461)]
    [EndpointResponse(462, typeof(ScopePayload))]
    [EndpointResponse(463, typeof(string), "text/plain", "application/json")]
    public sealed class FormatsEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/formats";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Ignored
{
    [EndpointResponse(470)]
    [UnrelatedResponse]
    public sealed class IgnoredGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/ignored");
    }

    [EndpointResponse(471)]
    [UnrelatedResponse]
    [EndpointFilter<PlainResponseFilter>]
    public sealed class IgnoredEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Fresh
{
    [EndpointResponse(480, typeof(string), "text/plain", "application/json")]
    public sealed class FreshGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/fresh");
    }

    [EndpointResponse(481, typeof(string), "text/plain", "application/json")]
    [EndpointFilter<PlainResponseFilter>]
    public sealed class FreshEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public FreshEndpoint() => throw new InvalidOperationException("Inspection must not construct endpoint targets.");

        [StringSyntax("Route")]
        public string Pattern => "/value";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Scoped
{
    [EndpointResponse(470)]
    public sealed class ScopedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/scoped");
    }

    [EndpointResponse(471)]
    public sealed class ScopedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Composed
{
    [EndpointResponse(472)]
    public sealed class ParentGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-parent");
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Composed.Child
{
    [EndpointResponse(473)]
    public sealed class ChildGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-child");
    }

    [EndpointResponse(474)]
    public sealed class ChildEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Disabled
{
    [EndpointResponse(496, typeof(string), "text/plain", null!)]
    [ThrowingScopeAuthorization]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled groups must not map.");
    }

    [EndpointResponse(496, typeof(string), "text/plain", null!)]
    public sealed class DisabledEndpoint : IGetEndpoint
    {
        public DisabledEndpoint() => throw new InvalidOperationException("Disabled targets must not activate.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled";

        public NoContent HandleAsync() => throw new InvalidOperationException("Disabled handlers must not run.");
    }

    [EndpointResponse(475)]
    public sealed class DisabledAliasEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/alias";

        public NoContent HandleAsync() => TypedResults.NoContent();
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Disabled.Child
{
    [EndpointResponse(496, typeof(string), "text/plain", null!)]
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("Disabled ancestors must suppress child conditions.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled child groups must not map.");
    }

    [EndpointResponse(496, typeof(string), "text/plain", null!)]
    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        public DisabledChildEndpoint() => throw new InvalidOperationException("Disabled children must not activate.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-child";

        public NoContent HandleAsync() => throw new InvalidOperationException("Disabled child handlers must not run.");
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Manual
{
    [EndpointResponse(496, typeof(string), "text/plain", null!)]
    public sealed class ManualMalformedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public ManualMalformedEndpoint() => throw new InvalidOperationException("Manual targets are excluded from automatic selection.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/manual";

        public NoContent HandleAsync() => throw new InvalidOperationException("Unmapped handlers must not run.");
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.Contracts
{
    [EndpointResponse(409, typeof(ScopeError))]
    public sealed class ContractGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/contracts");
    }

    [EndpointResponse(409, typeof(ScopePayload))]
    public sealed class ContractEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [EndpointResponse(409, typeof(string), "text/plain")]
        public Ok<ScopePayload> HandleAsync() => TypedResults.Ok(new ScopePayload("contract"));

        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.Produces<int>(409);
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.IntegrationTest.ScopeFixtures.MetadataOnly
{
    [EndpointResponse(418)]
    public sealed class FilterCapableEndpoint : IGetEndpoint, IManuallyMappedEndpoint, IEndpointFilter
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/metadata-only";

        public Ok<ScopePayload> HandleAsync(ScopeProbe probe)
        {
            probe.Handle();
            return TypedResults.Ok(new ScopePayload("actual-success"));
        }

        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            context.HttpContext.RequestServices.GetRequiredService<ScopeProbe>().Filter();
            return ValueTask.FromResult<object?>(TypedResults.BadRequest());
        }
    }
}