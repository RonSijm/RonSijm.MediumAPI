using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using static RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.EndpointRateLimitHost;

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedRatePolicyAttribute : Attribute, IRateLimiterPolicy<string>
    {
        public UnrelatedRatePolicyAttribute() => throw new InvalidOperationException("Unrelated rate-policy attributes must not be constructed.");
        public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => throw new InvalidOperationException("Unselected policies must not execute.");
        public RateLimitPartition<string> GetPartition(HttpContext context) => throw new InvalidOperationException("Unselected policies must not execute.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedMvcAttribute : Attribute, IAsyncActionFilter
    {
        public UnrelatedMvcAttribute() => throw new InvalidOperationException("Unrelated MVC attributes must not be constructed.");
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) => throw new InvalidOperationException("MVC filters do not execute through endpoint rate metadata.");
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
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) => builder.Metadata.Add(new EnableRateLimitingAttribute("scope-provider"));
        public Task ExecuteAsync(HttpContext context) => Reply(context.RequestServices.GetRequiredService<RateLimitProbe>(), "ordered").ExecuteAsync(context);
    }
}

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Ordered
{
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/ordered").RequireRateLimiting(RateLimitPolicies.Group);
    }

    [EnableRateLimiting(RateLimitPolicies.Class)]
    public sealed class OrderedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [EnableRateLimiting(RateLimitPolicies.Method)]
        public ScopeMetadataResult HandleAsync() => new();

        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.RequireRateLimiting(RateLimitPolicies.Fluent);
    }
}

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Inheritance
{
    [EnableRateLimiting(RateLimitPolicies.Class)]
    [DisableRateLimiting]
    public abstract class BaseClassEndpoint
    {
        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "base-class-ignored");
    }

    public sealed class BaseClassIgnoredEndpoint : BaseClassEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/base-class-ignored";
    }

    [DisableRateLimiting]
    public abstract class BaseMethodEndpoint
    {
        [EnableRateLimiting(RateLimitPolicies.Method)]
        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "inherited-method");
    }

    [EnableRateLimiting(RateLimitPolicies.Class)]
    public sealed class InheritedMethodEndpoint : BaseMethodEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/inherited-method";
    }
}

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Selected
{
    [EnableRateLimiting(RateLimitPolicies.Class)]
    [UnrelatedRatePolicy]
    [UnrelatedMvc]
    public sealed class IgnoredAttributesEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/ignored-attributes";

        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "ignored-attributes");
    }

    [EnableRateLimiting(RateLimitPolicies.Class)]
    public sealed class FreshEnableEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/fresh-enable";

        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "fresh-enable");
    }

    [DisableRateLimiting]
    public sealed class FreshDisableEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/fresh-disable";

        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "fresh-disable");
    }

    [EnableRateLimiting(RateLimitPolicies.Class)]
    public sealed class ActivationEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public ActivationEndpoint(RateLimitProbe probe)
        {
            probe.Record("activation-construction");
            throw new InvalidOperationException("Rate-rejected requests must not activate their endpoint target.");
        }

        [StringSyntax("Route")]
        public string Pattern => "/scope/activation";

        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "activation");
    }

    [EnableRateLimiting(RateLimitPolicies.Missing)]
    public sealed class MissingPolicyEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/missing-policy";

        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "missing-policy");
    }

    [DisableRateLimiting]
    public sealed class DisabledMissingPolicyEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-missing-policy";

        [EnableRateLimiting(RateLimitPolicies.Missing)]
        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "disabled-missing-policy");
    }

    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Class)]
    public sealed class MixedAuthorizationEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/mixed";

        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "mixed");
    }
}

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Scoped
{
    public sealed class ScopedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/scoped").DisableRateLimiting();
    }

    [EnableRateLimiting(RateLimitPolicies.Class)]
    public sealed class ScopedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "scoped");
    }
}

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Disabled
{
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled definitions must not map.");
    }

    [EnableRateLimiting(RateLimitPolicies.Class)]
    [ThrowingAuthorization]
    public sealed class DisabledEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled";

        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "disabled");
    }

    [EnableRateLimiting(RateLimitPolicies.Class)]
    public sealed class DisabledAliasEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-alias";

        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "disabled-alias");
    }
}

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Disabled.Child
{
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("A disabled parent's descendant condition must not run.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled descendants must not map.");
    }

    [DisableRateLimiting]
    [ThrowingAuthorization]
    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-child";

        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "disabled-child");
    }
}

namespace RonSijm.MediumAPI.EndpointRateLimitingExample.IntegrationTest.ScopeFixtures.Manual
{
    [EnableRateLimiting(RateLimitPolicies.Class)]
    [ThrowingAuthorization]
    public sealed class ManualThrowingEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/manual";

        public Ok<LimitResponse> HandleAsync(RateLimitProbe probe) => Reply(probe, "manual");
    }
}