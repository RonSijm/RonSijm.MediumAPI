using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResourcePoliciesExample.ArgumentGuards;

[EndpointFilter<IngressFilter>]
internal sealed class NamedArgumentGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/sample/argument-guards").WithTags("ArgumentGuards");
    }
}

internal sealed class UpdateGuardProfileEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/profile";

    [EndpointFilter<ExistingCommandOutcomeFilter>]
    [EndpointFilterFactory<ProfileArgumentGuardFactory>]
    public async Task<Results<BadRequest, Ok<GuardProfileDto>>> HandleAsync([FromBody] GuardProfileRequest request, NamedArgumentOperations operations, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await operations.UpdateProfileAsync(request.Profile!, request.Outcome, cancellationToken));
    }
}

internal sealed class ListGuardSupportEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/support";

    [EndpointResponse(200, typeof(IReadOnlyList<GuardSupportDto>), "application/json")]
    [EndpointResponse(400)]
    [EndpointFilter<ExistingReadOutcomeFilter>]
    [EndpointFilterFactory<SupportArgumentGuardFactory>]
    public async Task<IResult> HandleAsync([FromQuery(Name = "customer")] long? customerId, [FromQuery(Name = "project")] long? projectId, NamedArgumentOperations operations, [FromQuery] string? outcome, CancellationToken cancellationToken)
    {
        return Results.Ok(await operations.ListSupportAsync(customerId, projectId, outcome ?? "ok", cancellationToken));
    }
}

internal sealed class GetGuardOverviewEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/overview";

    [EndpointResponse(200)]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointFilter<ExistingCommandOutcomeFilter>]
    [EndpointFilterFactory<OverviewArgumentGuardFactory>]
    public async Task<IResult> HandleAsync(NamedArgumentOperations operations, [FromQuery] int? runningPage, [FromQuery] int? waitingPage, [FromQuery] int? finishedPage, [FromQuery] int? projectPage, [FromQuery] string? outcome, HttpContext context, CancellationToken cancellationToken)
    {
        var state = await operations.ReadOverviewAsync(outcome ?? "ok", cancellationToken);
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new { State = state, RunningPage = runningPage ?? 1, WaitingPage = waitingPage ?? 1, FinishedPage = finishedPage ?? 1, ProjectPage = projectPage ?? 1, Settings = await operations.ReadSettingsAsync(outcome ?? "ok", cancellationToken) });
    }
}