using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using static RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.EndpointAntiforgeryHost;

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DerivedAntiforgeryAttribute : RequireAntiforgeryTokenAttribute
    {
        public DerivedAntiforgeryAttribute() => throw new InvalidOperationException("Derived antiforgery attributes must not be constructed.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedAntiforgeryAttribute : Attribute, IAntiforgeryMetadata
    {
        public UnrelatedAntiforgeryAttribute() => throw new InvalidOperationException("Unrelated antiforgery attributes must not be constructed.");
        public bool RequiresValidation => throw new InvalidOperationException("Unselected metadata must not execute.");
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedMvcAttribute : Attribute, IAsyncActionFilter
    {
        public UnrelatedMvcAttribute() => throw new InvalidOperationException("Unrelated MVC attributes must not be constructed.");
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) => throw new InvalidOperationException("MVC action filters do not execute through endpoint antiforgery metadata.");
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
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder) => builder.Metadata.Add(new RequireAntiforgeryTokenAttribute(true));
        public Task ExecuteAsync(HttpContext context) => Reply("synthetic", "ordered", context.RequestServices.GetRequiredService<FormProbe>()).ExecuteAsync(context);
    }
}

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Ordered
{
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/ordered").WithMetadata(new RequireAntiforgeryTokenAttribute(true));
    }

    [RequireAntiforgeryToken(false)]
    public sealed class OrderedEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        [RequireAntiforgeryToken(false)]
        public ScopeMetadataResult HandleAsync() => new();

        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithMetadata(new RequireAntiforgeryTokenAttribute(true)).DisableAntiforgery();
    }
}

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Inheritance
{
    [RequireAntiforgeryToken]
    public abstract class BaseClassEndpoint
    {
        public Ok<FormReceipt> HandleAsync(FormProbe probe) => Reply("synthetic", "base-class-ignored", probe);
    }

    public sealed class BaseClassIgnoredEndpoint : BaseClassEndpoint, IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/base-class-ignored";
    }

    [RequireAntiforgeryToken]
    public abstract class BaseMethodEndpoint
    {
        [RequireAntiforgeryToken(false)]
        public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => Reply(message, "inherited-method", probe);
    }

    [RequireAntiforgeryToken]
    public sealed class InheritedMethodEndpoint : BaseMethodEndpoint, IPostEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/inherited-method";
    }
}

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Selected
{
    [RequireAntiforgeryToken(false)]
    [DerivedAntiforgery]
    [UnrelatedAntiforgery]
    [UnrelatedMvc]
    public sealed class IgnoredAttributesEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/ignored-attributes";

        public Ok<FormReceipt> HandleAsync(FormProbe probe) => Reply("synthetic", "ignored-attributes", probe);
    }

    [RequireAntiforgeryToken]
    public sealed class FreshTrueEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/fresh-true";

        public Ok<FormReceipt> HandleAsync(FormProbe probe) => Reply("synthetic", "fresh-true", probe);
    }

    [RequireAntiforgeryToken(false)]
    public sealed class FreshFalseEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/fresh-false";

        public Ok<FormReceipt> HandleAsync(FormProbe probe) => Reply("synthetic", "fresh-false", probe);
    }

    [RequireAntiforgeryToken]
    public sealed class ActivationEndpoint : IPostEndpoint, IManuallyMappedEndpoint, IDisposable
    {
        private readonly FormProbe probe;

        public ActivationEndpoint(FormProbe probe)
        {
            this.probe = probe;
            probe.Record("activation-construction");
        }

        [StringSyntax("Route")]
        public string Pattern => "/scope/activation";

        public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => Reply(message, "activation", probe);
        public void Dispose() => probe.Record("activation-disposal");
    }

    [RequireAntiforgeryToken]
    public sealed class NonFormRequiredGetEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/nonform-get";

        public Ok<FormReceipt> HandleAsync(FormProbe probe) => Reply("synthetic", "nonform-get", probe);
    }

    [RequireAntiforgeryToken]
    public sealed class NonFormRequiredPostEndpoint : IPostEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/nonform-post";

        public Ok<FormReceipt> HandleAsync(FormProbe probe) => Reply("synthetic", "nonform-post", probe);
    }
}

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Scoped
{
    public sealed class ScopedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope/scoped").DisableAntiforgery();
    }

    [RequireAntiforgeryToken]
    public sealed class ScopedEndpoint : IPostEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => Reply(message, "scoped", probe);
    }
}

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Composed
{
    public sealed class ParentGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-parent").DisableAntiforgery();
    }
}

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Composed.Child
{
    public sealed class ChildGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/scope-child");
    }

    [RequireAntiforgeryToken]
    public sealed class ChildEndpoint : IPostEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => Reply(message, "child", probe);
    }
}

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Disabled
{
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled definitions must not map.");
    }

    [RequireAntiforgeryToken]
    [ThrowingAuthorization]
    public sealed class DisabledEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled";

        public Ok<FormReceipt> HandleAsync(FormProbe probe) => Reply("synthetic", "disabled", probe);
    }

    [RequireAntiforgeryToken]
    public sealed class DisabledAliasEndpoint : IPostEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-alias";

        public Ok<FormReceipt> HandleAsync([FromForm] string message, FormProbe probe) => Reply(message, "disabled-alias", probe);
    }
}

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Disabled.Child
{
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("Disabled ancestor descendants must not evaluate conditions.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled descendants must not map.");
    }

    [RequireAntiforgeryToken(false)]
    [ThrowingAuthorization]
    public sealed class DisabledChildEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/disabled-child";

        public Ok<FormReceipt> HandleAsync(FormProbe probe) => Reply("synthetic", "disabled-child", probe);
    }
}

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Manual
{
    [RequireAntiforgeryToken]
    [ThrowingAuthorization]
    public sealed class ManualThrowingEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/scope/manual";

        public Ok<FormReceipt> HandleAsync(FormProbe probe) => Reply("synthetic", "manual", probe);
    }
}