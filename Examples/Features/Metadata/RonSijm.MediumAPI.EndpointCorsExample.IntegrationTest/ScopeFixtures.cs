using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest
{
    public sealed record ScopeReceipt(string Handler);

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DerivedCorsAttribute : EnableCorsAttribute
    {
        public DerivedCorsAttribute() => throw new InvalidOperationException("A derived CORS attribute must not be constructed through the exact native rule.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CustomCorsAttribute : Attribute, IEnableCorsAttribute
    {
        public CustomCorsAttribute() => throw new InvalidOperationException("Custom CORS metadata must not be constructed through the exact native rule.");

        public string? PolicyName
        {
            get => throw new InvalidOperationException("Unsupported CORS getters must not be read.");
            set => throw new InvalidOperationException("Unsupported CORS setters must not be invoked.");
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedMvcAttribute : Attribute, IAsyncActionFilter
    {
        public UnrelatedMvcAttribute() => throw new InvalidOperationException("Unrelated MVC attributes must not be constructed.");
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) => throw new InvalidOperationException("MVC filters do not execute through this feature.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CompatibleCorsAuthorizationAttribute : EnableCorsAttribute, IAuthorizeData
    {
        public CompatibleCorsAuthorizationAttribute() : base(SamplePolicies.Browser) { }

        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ThrowingAuthorizationAttribute : Attribute, IAuthorizeData
    {
        public ThrowingAuthorizationAttribute() => throw new InvalidOperationException("Suppressed class metadata must not be constructed.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }

    public sealed class ScopeMetadataResult : IResult, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) => builder.Metadata.Add(new EnableCorsAttribute(SamplePolicies.Override));
        public Task ExecuteAsync(HttpContext context) => TypedResults.Ok(new ScopeReceipt("ordered")).ExecuteAsync(context);
    }
}

namespace RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.ScopeFixtures.Ordered
{
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/ordered").RequireCors(SamplePolicies.Browser);
    }

    [DisableCors]
    public sealed class OrderedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [EnableCors(SamplePolicies.Browser)]
        public ScopeMetadataResult HandleAsync() => new();

        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithMetadata(new DisableCorsAttribute());
    }
}

namespace RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.ScopeFixtures.Inheritance
{
    [EnableCors("BaseClassPolicyMustNotBeImported")]
    public abstract class BaseEndpoint
    {
        [EnableCors(SamplePolicies.Override)]
        public Ok<ScopeReceipt> HandleAsync() => TypedResults.Ok(new ScopeReceipt("inherited"));
    }

    [EnableCors(SamplePolicies.Browser)]
    public sealed class DerivedEndpoint : BaseEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/inherited";
    }

    public sealed class BaseClassIgnoredEndpoint : BaseEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/base-class-ignored";
    }
}

namespace RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.ScopeFixtures.Exact
{
    [DerivedCors]
    [CustomCors]
    [UnrelatedMvc]
    public sealed class IgnoredEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/ignored";

        public Ok<ScopeReceipt> HandleAsync() => TypedResults.Ok(new ScopeReceipt("ignored"));
    }

    [CompatibleCorsAuthorization(Policy = "ScopePolicy")]
    public sealed class CompatibleAuthorizationEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/compatible-authorization";

        public Ok<ScopeReceipt> HandleAsync() => TypedResults.Ok(new ScopeReceipt("compatible"));
    }

    [EnableCors(PolicyName = SamplePolicies.Browser)]
    public sealed class SetterEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/setter";

        public Ok<ScopeReceipt> HandleAsync() => TypedResults.Ok(new ScopeReceipt("setter"));
    }

    [EnableCors(SamplePolicies.Browser)]
    public sealed class PreflightOnlyEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public PreflightOnlyEndpoint() => throw new InvalidOperationException("A preflight must not construct the endpoint instance.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/preflight-only";

        public Ok<ScopeReceipt> HandleAsync() => TypedResults.Ok(new ScopeReceipt("unreachable"));
    }

    [EnableCors("NotRegistered")]
    public sealed class MissingPolicyEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/missing-policy";

        public Ok<ScopeReceipt> HandleAsync() => TypedResults.Ok(new ScopeReceipt("missing-policy"));
    }

    [Authorize]
    [EnableCors(SamplePolicies.Browser)]
    public sealed class MixedAuthorizationEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/mixed";

        public Ok<ScopeReceipt> HandleAsync() => TypedResults.Ok(new ScopeReceipt("mixed"));
    }
}

namespace RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.ScopeFixtures.Scoped
{
    [EnableCors(SamplePolicies.Override)]
    public sealed class ScopedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/scoped");
    }

    [EnableCors(SamplePolicies.Browser)]
    public sealed class ScopedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<ScopeReceipt> HandleAsync() => TypedResults.Ok(new ScopeReceipt("scoped"));
    }
}

namespace RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.ScopeFixtures.Disabled
{
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("A disabled definition must not map.");
    }

    [EnableCors(SamplePolicies.Browser)]
    [ThrowingAuthorization]
    public sealed class DisabledEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled";

        public Ok<ScopeReceipt> HandleAsync() => TypedResults.Ok(new ScopeReceipt("disabled"));
    }

    [EnableCors(SamplePolicies.Browser)]
    public sealed class DisabledAliasEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-alias";

        public Ok<ScopeReceipt> HandleAsync() => TypedResults.Ok(new ScopeReceipt("disabled-alias"));
    }
}

namespace RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.ScopeFixtures.Disabled.Child
{
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("A disabled parent's descendant condition must not run.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("A disabled descendant must not map.");
    }

    [EnableCors(SamplePolicies.Browser)]
    [ThrowingAuthorization]
    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-child";

        public Ok<ScopeReceipt> HandleAsync() => TypedResults.Ok(new ScopeReceipt("disabled child"));
    }
}