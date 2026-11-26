using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResourcePoliciesExample;

// The legacy wrappers have independent catch lists and literals, beneath the same retained outer policies.
public static class LegacyOperationOutcomePolicies
{
    public static async Task<Results<Created<CustomerOutcomeDto>, ForbidHttpResult, ProblemHttpResult>> CustomerAsync(Func<Task<Created<CustomerOutcomeDto>>> operation)
    {
        try { return await operation(); }
        catch (AccessDeniedException) { return TypedResults.Forbid(); }
        catch (ReplayConflictException exception) { return Replay(exception); }
        catch (InvalidResourceException) { return Problem(400, "invalid-request", "The request could not be completed."); }
    }

    public static async Task<IResult> ReplayAsync(Func<Task<IResult>> operation)
    {
        try { return await operation(); }
        catch (ReplayConflictException exception) { return Replay(exception); }
    }

    public static async Task<Results<NoContent, NotFound, ProblemHttpResult>> CompletedAsync(Func<Task<Results<NoContent, NotFound, ProblemHttpResult>>> operation)
    {
        try { return await operation(); }
        catch (CompletedOperationException) { return Problem(409, "operation-already-completed", "The operation cannot be cancelled."); }
    }

    public static async Task<IResult> StaleAsync(Func<Task<IResult>> operation)
    {
        try { return await operation(); }
        catch (StaleResourceException) { return Problem(409, "stale-catalog-entry", "Reload the current entry before saving."); }
    }

    public static async Task<Results<Accepted<GenerationOutcomeDto>, NotFound, ProblemHttpResult>> ValidationAsync(Func<Task<Results<Accepted<GenerationOutcomeDto>, NotFound, ProblemHttpResult>>> operation)
    {
        try { return await operation(); }
        catch (InvalidResourceException exception) { return Problem(400, exception.Code ?? "invalid-request", "The request could not be completed."); }
    }

    private static ProblemHttpResult Replay(ReplayConflictException exception)
    {
        return Problem(409, exception.Code, exception.Code switch
        {
            "idempotency-request-in-progress" => "A request with this idempotency key is still being processed.",
            "idempotency-one-time-receipt-issued" => "The token was created. Check saved tokens; its one-time secret cannot be shown again.",
            _ => "This idempotency key was already used for a different request."
        });
    }

    private static ProblemHttpResult Problem(int status, string code, string title)
    {
        return TypedResults.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code });
    }
}

public static class LegacyOperationOutcomes
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/sample/outcomes").WithTags("OperationOutcomes").AddEndpointFilter<IngressFilter>().AddEndpointFilter<ObserveResourceFilter>();
        MapOriginal(group, "/customers", CreateCustomer).WithName("CreateOutcomeCustomer");
        MapOriginal(group, "/projects", CreateProject).WithName("CreateOutcomeProject").AddMediumApiFilter<ExistingRequestOutcomeFilter>();
        MapOriginal(group, "/reports", QueueReport).WithName("QueueOutcomeReport").AddMediumApiFilter<ExistingCommandOutcomeFilter>();
        MapOriginal(group, "/operations/{resourceId:int}/cancel", CancelOperation).WithName("CancelOutcomeOperation").AddMediumApiFilter<ExistingReadOutcomeFilter>();
        MapOriginal(group, "/catalog", UpsertCatalog).WithName("UpsertOutcomeCatalog").AddMediumApiFilter<ExistingRequestOutcomeFilter>();
        MapOriginal(group, "/intake/{resourceId:int}", QueueIntake).WithName("QueueOutcomeIntake").AddMediumApiFilter<ExistingReadOutcomeFilter>();
    }

    [EndpointResponse(400)]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(409)]
    public static Task<Results<Created<CustomerOutcomeDto>, ForbidHttpResult, ProblemHttpResult>> CreateCustomer([FromBody] OperationOutcomeCommand request, OperationOutcomeService operations, HttpContext context, CancellationToken cancellationToken)
    {
        return LegacyOperationOutcomePolicies.CustomerAsync(async () =>
        {
            var outcome = await operations.ExecuteAsync(request.Outcome, cancellationToken);
            if (outcome.Replayed) { context.Response.Headers["Idempotent-Replayed"] = "true"; }

            return TypedResults.Created($"/sample/outcomes/customers/{outcome.Id}", new CustomerOutcomeDto(outcome.Id));
        });
    }

    [EndpointResponse(201, contentType: "application/json")]
    [EndpointResponse(409, typeof(ProblemDetails), "application/problem+json")]
    public static Task<IResult> CreateProject([FromBody] OperationOutcomeCommand request, OperationOutcomeService operations, HttpContext context, CancellationToken cancellationToken)
    {
        return LegacyOperationOutcomePolicies.ReplayAsync(async () =>
        {
            var outcome = await operations.ExecuteAsync(request.Outcome, cancellationToken);
            if (outcome.Replayed) { context.Response.Headers["Idempotent-Replayed"] = "true"; }

            return Results.Created($"/sample/outcomes/projects/{outcome.Id}", new { id = outcome.Id });
        });
    }

    [EndpointResponse(200)]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(404, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(409, typeof(ProblemDetails), "application/problem+json")]
    public static async Task<IResult> QueueReport([FromBody] OperationOutcomeCommand request, OperationOutcomeService operations, HttpContext context, CancellationToken cancellationToken)
    {
        var result = await LegacyOperationOutcomePolicies.ReplayAsync(async () =>
        {
            var outcome = await operations.ExecuteAsync(request.Outcome, cancellationToken);
            if (outcome.Replayed) { context.Response.Headers["Idempotent-Replayed"] = "true"; }

            return Results.Ok(new { ReportId = outcome.Id, AnalysisRunId = 5L, ProjectVersionId = 3L });
        });
        if (IsSuccess(result)) { context.Response.Headers.CacheControl = "no-store"; }

        return result;
    }

    [EndpointResponse(409)]
    public static Task<Results<NoContent, NotFound, ProblemHttpResult>> CancelOperation([FromRoute] int resourceId, [FromBody] OperationOutcomeCommand request, OperationOutcomeService operations, CancellationToken cancellationToken)
    {
        return LegacyOperationOutcomePolicies.CompletedAsync(async () =>
        {
            if (!await operations.OwnsAsync(request.Outcome, cancellationToken)) { return TypedResults.NotFound(); }

            await operations.ExecuteAsync(request.Outcome, cancellationToken);
            return TypedResults.NoContent();
        });
    }

    [EndpointResponse(409, contentType: "application/json")]
    [EndpointResponse(200, contentType: "application/json")]
    [EndpointResponse(201, contentType: "application/json")]
    public static async Task<IResult> UpsertCatalog([FromBody] OperationOutcomeCommand request, OperationOutcomeService operations, HttpContext context, CancellationToken cancellationToken)
    {
        var result = await LegacyOperationOutcomePolicies.StaleAsync(async () =>
        {
            var outcome = await operations.ExecuteAsync(request.Outcome, cancellationToken);
            return request.Update ? Results.Ok(new { id = outcome.Id, revision = 3L }) : Results.Created($"/sample/outcomes/catalog/{outcome.Id}", new { id = outcome.Id, revision = 3L });
        });
        if (IsSuccess(result)) { context.Response.Headers.CacheControl = "no-store"; }

        return result;
    }

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    public static Task<Results<Accepted<GenerationOutcomeDto>, NotFound, ProblemHttpResult>> QueueIntake([FromRoute] int resourceId, [FromBody] OperationOutcomeCommand request, OperationOutcomeService operations, CancellationToken cancellationToken)
    {
        return LegacyOperationOutcomePolicies.ValidationAsync(async () =>
        {
            if (!await operations.OwnsAsync(request.Outcome, cancellationToken)) { return TypedResults.NotFound(); }

            var outcome = await operations.ExecuteAsync(request.Outcome, cancellationToken);
            return TypedResults.Accepted($"/sample/outcomes/intake/{resourceId}/generation", new GenerationOutcomeDto(outcome.Id, "Queued"));
        });
    }

    private static bool IsSuccess(IResult result)
    {
        if (!result.GetType().IsGenericType) { return false; }

        var definition = result.GetType().GetGenericTypeDefinition();
        return definition == typeof(Ok<>) || definition == typeof(Created<>);
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapMethods(pattern, ["POST"], handler);
    }
}