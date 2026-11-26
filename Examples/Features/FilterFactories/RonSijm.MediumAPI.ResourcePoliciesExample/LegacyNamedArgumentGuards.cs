using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResourcePoliciesExample;

// Independent inline preconditions are retained below the same native outer policies.
public static class LegacyNamedArgumentGuards
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/sample/argument-guards").WithTags("ArgumentGuards").AddEndpointFilter<IngressFilter>();
        MapOriginal(group, "/profile", Profile, "POST").WithName("UpdateGuardProfile").AddMediumApiFilter<ExistingCommandOutcomeFilter>();
        MapOriginal(group, "/support", Support, "GET").WithName("ListGuardSupport").AddMediumApiFilter<ExistingReadOutcomeFilter>();
        MapOriginal(group, "/overview", Overview, "GET").WithName("GetGuardOverview").AddMediumApiFilter<ExistingCommandOutcomeFilter>();
    }

    public static async Task<Results<BadRequest, Ok<GuardProfileDto>>> Profile([FromBody] GuardProfileRequest request, NamedArgumentOperations operations, CancellationToken cancellationToken)
    {
        if (request.Profile is null) { return TypedResults.BadRequest(); }

        return TypedResults.Ok(await operations.UpdateProfileAsync(request.Profile, request.Outcome, cancellationToken));
    }

    [EndpointResponse(200, typeof(IReadOnlyList<GuardSupportDto>), "application/json")]
    [EndpointResponse(400)]
    public static async Task<IResult> Support([FromQuery(Name = "customer")] long? customerId, [FromQuery(Name = "project")] long? projectId, NamedArgumentOperations operations, [FromQuery] string? outcome, CancellationToken cancellationToken)
    {
        if (!customerId.HasValue && projectId.HasValue) { return Results.BadRequest(new { error = "projectId requires customerId." }); }

        return Results.Ok(await operations.ListSupportAsync(customerId, projectId, outcome ?? "ok", cancellationToken));
    }

    [EndpointResponse(200)]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    public static async Task<IResult> Overview(NamedArgumentOperations operations, [FromQuery] int? runningPage, [FromQuery] int? waitingPage, [FromQuery] int? finishedPage, [FromQuery] int? projectPage, [FromQuery] string? outcome, HttpContext context, CancellationToken cancellationToken)
    {
        foreach (var page in new[] { runningPage, waitingPage, finishedPage, projectPage })
        {
            if (page is <= 0 or > 100000) { throw new InvalidResourceException(); }
        }

        var state = await operations.ReadOverviewAsync(outcome ?? "ok", cancellationToken);
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new { State = state, RunningPage = runningPage ?? 1, WaitingPage = waitingPage ?? 1, FinishedPage = finishedPage ?? 1, ProjectPage = projectPage ?? 1, Settings = await operations.ReadSettingsAsync(outcome ?? "ok", cancellationToken) });
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler, string method)
    {
        return endpoints.MapMethods(pattern, [method], handler);
    }
}