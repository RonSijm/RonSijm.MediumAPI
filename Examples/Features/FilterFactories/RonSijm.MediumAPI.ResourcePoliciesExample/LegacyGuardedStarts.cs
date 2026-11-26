using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResourcePoliciesExample;

public static class LegacyGuardedStarts
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        MapOriginal(endpoints, "/sample/guarded-start", Start).WithName("GuardedStart").AddEndpointFilter<IngressFilter>();
        MapOriginal(endpoints, "/sample/shifted-start", ShiftedStart).WithName("ShiftedGuardedStart").AddEndpointFilter<IngressFilter>();
        MapOriginal(endpoints, "/sample/start-alias", ShiftedStart).AddEndpointFilter<IngressFilter>();
        MapOriginal(endpoints, "/sample/factory-namespace/start", NamespaceStart).WithName("NamespaceGuardedStart").AddEndpointFilter<IngressFilter>();
        endpoints.MapGet("/sample/factory-repeated", Repeated).WithName("RepeatedDeclaration").AddEndpointFilterFactory(RepeatedFactory.Create).AddEndpointFilter<FactoryOrderFilter>().AddEndpointFilterFactory(RepeatedFactory.Create);
    }

    public static Ok<int> Repeated(HttpContext context)
    {
        return TypedResults.Ok((int)context.Items["factory-count"]!);
    }

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(409, typeof(ProblemDetails), "application/problem+json")]
    public static Task<Results<Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> Start([FromBody] GuardedStartRequest request, OperationOutcomeService operations, FactoryRequestScope scope, HttpContext context, CancellationToken cancellationToken)
    {
        return BeforeCommand(request, operations, scope, context, cancellationToken);
    }

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(409, typeof(ProblemDetails), "application/problem+json")]
    public static Task<Results<Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> ShiftedStart(HttpContext context, FactoryRequestScope scope, [FromBody] GuardedStartRequest request, OperationOutcomeService operations, CancellationToken cancellationToken)
    {
        return BeforeCommand(request, operations, scope, context, cancellationToken);
    }

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(409, typeof(ProblemDetails), "application/problem+json")]
    public static Task<Results<Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> NamespaceStart(OperationOutcomeService operations, [FromBody] GuardedStartRequest request, HttpContext context, FactoryRequestScope scope, CancellationToken cancellationToken)
    {
        return BeforeCommand(request, operations, scope, context, cancellationToken);
    }

    public static Task<Results<Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> BeforeCommand(GuardedStartRequest request, OperationOutcomeService operations, FactoryRequestScope scope, HttpContext context, CancellationToken cancellationToken)
    {
        if (context.Request.Headers["X-Replace-Selection"] == "true") { request = request with { Items = new[] { 41, 43 } }; }

        // The independent old early check is outside every command catch.
        if (request.Items is null || request.Items.Count == 0)
        {
            return Task.FromResult<Results<Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>>(TypedResults.Problem(statusCode: 400, title: "Select at least one item.", extensions: new Dictionary<string, object?> { ["code"] = "invalid-selection" }));
        }

        context.Response.Headers["X-Factory-Scope"] = scope.Id.ToString();
        return CommandAsync(request, operations, scope, context, cancellationToken);
    }

    private static async Task<Results<Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> CommandAsync(GuardedStartRequest request, OperationOutcomeService operations, FactoryRequestScope scope, HttpContext context, CancellationToken cancellationToken)
    {
        try
        {
            Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult> result;
            if (!await operations.OwnsAsync(request.Outcome == "late-conflict" ? "ok" : request.Outcome, cancellationToken))
            {
                result = TypedResults.NotFound();
            }
            else
            {
                try
                {
                    var outcome = await operations.ExecuteAsync(request.Outcome == "late-conflict" ? "conflict" : request.Outcome, cancellationToken);
                    if (outcome.Replayed) { context.Response.Headers["Idempotent-Replayed"] = "true"; }

                    result = TypedResults.Ok(new GuardedStartedDto(outcome.Id, request.Items!.Count, scope.Id));
                }
                catch (ReplayConflictException exception)
                {
                    result = Replay(exception);
                }
            }

            return result;
        }
        catch (AccessDeniedException) { return TypedResults.Forbid(); }
        catch (MissingResourceException) { return TypedResults.NotFound(); }
        catch (ReplayConflictException exception) { return Replay(exception); }
        catch (StaleResourceException) { return Problem(409, "concurrency-conflict", "The item was changed by someone else. Reload and try again."); }
        catch (InvalidResourceException exception) { return Problem(400, exception.Code ?? "invalid-request", "The request could not be completed."); }
        catch (CompletedOperationException) { return Problem(400, "invalid-request", "The request could not be completed."); }
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

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapMethods(pattern, new[] { "POST" }, handler);
    }
}