using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest
{
    public enum ScopeProbeKind
    {
        Constructor,
        Named
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class ProbeAuthorizeAttribute : Attribute, IAuthorizeData
    {
        public ProbeAuthorizeAttribute(ScopeProbeKind kind, string[] labels)
        {
            Kind = kind;
            Labels = labels;
        }

        public ScopeProbeKind Kind { get; }
        public string[] Labels { get; }
        public ScopeProbeKind NamedKind;
        public string[] NamedLabels = [];
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ProbeAnonymousAttribute(string[] labels) : Attribute, IAllowAnonymous
    {
        public string[] Labels { get; } = labels;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedMvcFilterAttribute : Attribute, IAsyncActionFilter
    {
        public UnrelatedMvcFilterAttribute() => throw new InvalidOperationException("Unrelated class attributes must not be constructed.");
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) => throw new InvalidOperationException("MVC action filters are not executed by this feature.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ThrowingAuthorizeAttribute : Attribute, IAuthorizeData
    {
        public ThrowingAuthorizeAttribute() => throw new InvalidOperationException("Selected authorization attribute construction failed.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }

    public sealed class ScopeMetadataResult : IResult, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) => builder.Metadata.Add(new AuthorizeAttribute(SamplePolicies.Audit));
        public Task ExecuteAsync(HttpContext context) => TypedResults.Ok(new AuthorizationReceipt("ordered", context.User.Identity?.IsAuthenticated == true)).ExecuteAsync(context);
    }
}

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.ScopeFixtures.Ordered
{
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/ordered").RequireAuthorization(SamplePolicies.GroupAccess);
    }

    [Authorize(SamplePolicies.Readers)]
    public sealed class OrderedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public string? AuthorizationPolicy => SamplePolicies.Readers;

        [Authorize(SamplePolicies.Writers)]
        public ScopeMetadataResult HandleAsync() => new();

        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.RequireAuthorization(SamplePolicies.Audit);
    }
}

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.ScopeFixtures.Inheritance
{
    [Authorize(SamplePolicies.GroupAccess)]
    public abstract class BaseEndpoint
    {
        [Authorize(SamplePolicies.Readers)]
        public Ok<AuthorizationReceipt> HandleAsync(HttpContext context) => TypedResults.Ok(new AuthorizationReceipt("inherited", context.User.Identity?.IsAuthenticated == true));
    }

    [Authorize(SamplePolicies.Writers)]
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

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.ScopeFixtures.Custom
{
    [ProbeAuthorize(ScopeProbeKind.Constructor, new[] { "first", "second" }, NamedKind = ScopeProbeKind.Named, NamedLabels = new[] { "named" }, Policy = SamplePolicies.Readers, Roles = "Reader", AuthenticationSchemes = SamplePolicies.PrimaryScheme)]
    [UnrelatedMvcFilter]
    public sealed class CustomMetadataEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        public CustomMetadataEndpoint() => throw new InvalidOperationException("Endpoint instances must not be created by metadata inspection or a rejected request.");

        [StringSyntax("Route")]
        public string Pattern => "/scope/custom";

        public Ok<AuthorizationReceipt> HandleAsync(HttpContext context) => TypedResults.Ok(new AuthorizationReceipt("custom", context.User.Identity?.IsAuthenticated == true));
    }

    [ProbeAnonymous(new[] { "public" })]
    public sealed class CustomAnonymousEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/custom-anonymous";

        public Ok<AuthorizationReceipt> HandleAsync(HttpContext context) => TypedResults.Ok(new AuthorizationReceipt("custom-anonymous", context.User.Identity?.IsAuthenticated == true));
    }

    [ThrowingAuthorize]
    public sealed class ThrowingMetadataEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/throwing";

        public Ok<string> HandleAsync() => TypedResults.Ok("unreachable");
    }
}

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.ScopeFixtures.Scoped
{
    public sealed class ScopedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/scoped").RequireAuthorization(SamplePolicies.GroupAccess);
    }

    [Authorize(SamplePolicies.Readers)]
    public sealed class ScopedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<AuthorizationReceipt> HandleAsync(HttpContext context) => TypedResults.Ok(new AuthorizationReceipt("scoped", context.User.Identity?.IsAuthenticated == true));
    }
}

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.ScopeFixtures.Disabled
{
    [Authorize(SamplePolicies.GroupAccess)]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled definitions must not map.");
    }

    [ThrowingAuthorize]
    public sealed class DisabledEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled";

        public Ok<string> HandleAsync() => TypedResults.Ok("disabled");
    }

    [Authorize(SamplePolicies.Readers)]
    public sealed class DisabledAliasEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-alias";

        public Ok<AuthorizationReceipt> HandleAsync(HttpContext context) => TypedResults.Ok(new AuthorizationReceipt("disabled-alias", context.User.Identity?.IsAuthenticated == true));
    }
}

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.ScopeFixtures.Disabled.Child
{
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("A disabled parent's descendant must not evaluate.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled descendants must not map.");
    }

    [ThrowingAuthorize]
    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-child";

        public Ok<string> HandleAsync() => TypedResults.Ok("disabled child");
    }
}