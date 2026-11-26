using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.ConventionEndpoints;

[EndpointConvention<DocumentScopeConvention>]
public sealed class ConventionGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        return context.Services.GetRequiredService<IConfiguration>().GetValue("Sample:StartupConventions", false);
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/conventions").WithGroupName(DocumentationGroups.Public).WithTags("Startup conventions");
    }
}

[EndpointConvention<TokenResponsesConvention>]
internal sealed class ConventionTokenEndpoint : IGetEndpoint
{
    public ConventionTokenEndpoint(StartupConventionProbe probe)
    {
        probe.Constructions++;
    }

    [StringSyntax("Route")]
    public string Pattern => "/token";

    public JsonHttpResult<SampleTokenResponse> HandleAsync(StartupConventionProbe probe)
    {
        probe.Calls++;
        return TypedResults.Json(new SampleTokenResponse("Sample-Header", "fixed-synthetic-token"));
    }
}

internal sealed class ConventionAuthenticationEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/authenticate";

    [EndpointConvention<AuthenticationResponsesConvention>]
    public IResult HandleAsync(HttpContext context, StartupConventionProbe probe)
    {
        probe.Calls++;
        return context.GetEndpoint()?.Metadata.GetMetadata<SamplePresentation>()?.UsesJsonResponses == true ? Results.Ok(new SampleAuthenticationResponse("ready", null)) : Results.Redirect("/sign-in");
    }
}

[EndpointConvention<RequestOptionsResponsesConvention>]
internal sealed class ConventionRequestOptionsEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/request-options";

    public IResult HandleAsync(StartupConventionProbe probe)
    {
        probe.Calls++;
        return Results.Json(new { challenge = "fixed-synthetic-challenge" });
    }
}

[EndpointConvention<ChallengeResponsesConvention>]
internal sealed class ConventionChallengeEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/challenge";

    public RedirectHttpResult HandleAsync(StartupConventionProbe probe)
    {
        probe.Calls++;
        return TypedResults.Redirect("/external-sign-in");
    }
}

[EndpointConvention<LogoutResponsesConvention>]
internal sealed class ConventionLogoutEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/logout";

    public Results<BadRequest, ProblemHttpResult, RedirectHttpResult, NoContent> HandleAsync(HttpContext context, StartupConventionProbe probe)
    {
        probe.Calls++;
        return context.GetEndpoint()?.Metadata.GetMetadata<SamplePresentation>()?.UsesJsonResponses == true ? TypedResults.NoContent() : TypedResults.Redirect("/signed-out");
    }
}

internal sealed class ConventionWorkspaceEndpoint : IPostEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/conventions/workspace";

    public Results<BadRequest, NotFound, RedirectHttpResult, NoContent> HandleAsync(HttpContext context, StartupConventionProbe probe)
    {
        probe.Calls++;
        return context.GetEndpoint()?.Metadata.GetMetadata<SamplePresentation>()?.UsesJsonResponses == true ? TypedResults.NoContent() : TypedResults.Redirect("/workspace");
    }
}