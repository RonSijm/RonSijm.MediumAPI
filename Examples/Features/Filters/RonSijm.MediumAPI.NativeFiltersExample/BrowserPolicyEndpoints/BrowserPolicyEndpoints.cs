using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.NativeFiltersExample.BrowserPolicyEndpoints
{
    [EndpointConvention<BrowserWorkspaceConvention>]
    [ResponseHeader("CacheControl", "no-store")]
    [ResponseHeader("Pragma", "no-cache")]
    public sealed class BrowserPolicyGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return context.Services.GetRequiredService<IConfiguration>().GetValue("Sample:BrowserPolicies", false);
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/browser").WithTags("Browser policies");
        }
    }

    public sealed class BrowserReadEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/read/{value:int}";

        public Ok<BrowserReceipt> HandleAsync([FromRoute] int value, RequestTrace trace, BrowserPolicyProbe probe)
        {
            probe.HandlerCalls++;
            return TypedResults.Ok(new BrowserReceipt(value, trace.ScopeId));
        }
    }

    public sealed class BrowserTokenEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/token";

        public Ok<BrowserTokenResponse> HandleAsync(HttpContext context, IAntiforgery antiforgery)
        {
            return TypedResults.Ok(new BrowserTokenResponse(antiforgery.GetAndStoreTokens(context).RequestToken!));
        }
    }

    public sealed class BrowserValueEndpoint : IPostEndpoint, IManuallyMappedEndpoint
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

namespace RonSijm.MediumAPI.NativeFiltersExample.BrowserPolicyEndpoints.Disabled
{
    public sealed class UnreachableBrowserConvention : IMediumApiEndpointConvention
    {
        public static void Configure(IEndpointConventionBuilder builder)
        {
            throw new InvalidOperationException("A disabled namespace must not configure a policy.");
        }
    }

    [EndpointConvention<UnreachableBrowserConvention>]
    public sealed class DisabledBrowserGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return false;
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            throw new InvalidOperationException("A disabled namespace must not map.");
        }
    }
}