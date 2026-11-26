using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest.Fixtures
{
    public enum ProbeKind
    {
        Constructor,
        Named
    }

    public sealed record OrderMarker(string Stage);

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class ProbeAuthorizeAttribute : Attribute, IAuthorizeData
    {
        public ProbeAuthorizeAttribute(ProbeKind kind, string[] labels)
        {
            Kind = kind;
            Labels = labels;
        }

        public Guid InstanceId { get; } = Guid.NewGuid();
        public ProbeKind Kind { get; }
        public string[] Labels { get; }
        public ProbeKind NamedKind;
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedMvcFilterAttribute : Attribute, IAsyncActionFilter
    {
        public UnrelatedMvcFilterAttribute() => throw new InvalidOperationException("Unsupported group attributes must not be constructed.");
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) => next();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ThrowingAuthorizeAttribute : Attribute, IAuthorizeData
    {
        public ThrowingAuthorizeAttribute() => throw new InvalidOperationException("Disabled namespace policies must not be constructed.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }
}

namespace RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest.Fixtures.Probe
{
    [Authorize]
    [ProbeAuthorize(ProbeKind.Constructor, new[] { "first", "second" }, NamedKind = ProbeKind.Named, Policy = ExamplePolicies.Readers)]
    [EnableCors]
    [UnrelatedMvcFilter]
    public sealed class ProbeGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/policy-probe").WithMetadata(new OrderMarker("Map"));
    }

    public sealed class ProbeEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public IResult HandleAsync() => Results.Ok("probe");
        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithMetadata(new OrderMarker("Endpoint"));
    }
}

namespace RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest.Fixtures.Disabled
{
    [ThrowingAuthorize]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled group Map must not execute.");
    }

    public sealed class DisabledEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/disabled-parent";

        public IResult HandleAsync() => Results.Ok("disabled");
    }
}

namespace RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest.Fixtures.Disabled.Child
{
    [ThrowingAuthorize]
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("Disabled parent's child condition must not execute.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled child's Map must not execute.");
    }

    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/disabled-child";

        public IResult HandleAsync() => Results.Ok("disabled child");
    }
}

namespace RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest.Fixtures.ClassInheritance
{
    [Authorize(Policy = "NotRegisteredBecauseBaseClassAttributesAreIgnored")]
    [EnableCors("NotRegisteredBecauseBaseClassAttributesAreIgnored")]
    public abstract class BasePolicies
    {
        protected BasePolicies() => throw new InvalidOperationException("Namespace group definitions must not be instantiated.");
    }

    public sealed class DerivedGroupDefinition : BasePolicies, INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/class-inheritance");
    }

    public sealed class DerivedEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public IResult HandleAsync() => Results.Ok("declared attributes only");
    }
}