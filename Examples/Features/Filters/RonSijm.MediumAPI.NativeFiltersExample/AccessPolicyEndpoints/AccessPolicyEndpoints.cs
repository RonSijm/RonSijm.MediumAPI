using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.NativeFiltersExample.AccessPolicyEndpoints
{
    [EndpointConvention<WorkspaceAccessConvention>]
    [ResponseHeader("CacheControl", "no-store")]
    public sealed class AccessPolicyGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return context.Services.GetRequiredService<IConfiguration>().GetValue("Sample:AccessPolicies", false);
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/access").WithTags("Access policies");
        }
    }

    public sealed class AccessReadEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/read/{value:int}";

        public Ok<BrowserReceipt> HandleAsync([FromRoute] int value, RequestTrace trace, BrowserPolicyProbe probe)
        {
            probe.HandlerCalls++;
            return TypedResults.Ok(new BrowserReceipt(value, trace.ScopeId));
        }
    }

    public sealed class AccessValueEndpoint : IPostEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value/{value:int}";

        public Results<Ok<BrowserReceipt>, BadRequest<BrowserError>> HandleAsync([FromRoute] int value, [FromQuery] string? outcome, RequestTrace trace, BrowserPolicyProbe probe)
        {
            probe.HandlerCalls++;
            if (outcome == "failure") { throw new BrowserPolicyFailure(); }

            if (outcome == "reject") { return TypedResults.BadRequest(new BrowserError("downstream-rejected")); }

            return TypedResults.Ok(new BrowserReceipt(value, trace.ScopeId));
        }
    }
}

namespace RonSijm.MediumAPI.NativeFiltersExample.AccessPolicyEndpoints.Disabled
{
    public readonly struct UnreachableNestedConvention : IMediumApiEndpointConvention
    {
        public static void Configure(IEndpointConventionBuilder builder)
        {
            builder.AddMediumApiConvention<BrowserPolicyEndpoints.Disabled.UnreachableBrowserConvention>();
        }
    }

    [EndpointConvention<UnreachableNestedConvention>]
    public sealed class DisabledAccessPolicyGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return false;
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            throw new InvalidOperationException("A disabled access namespace must not map.");
        }
    }
}