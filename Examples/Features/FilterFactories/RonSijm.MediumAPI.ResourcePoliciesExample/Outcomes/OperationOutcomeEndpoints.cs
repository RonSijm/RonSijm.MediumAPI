using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResourcePoliciesExample.Outcomes;

[EndpointFilter<IngressFilter>]
[EndpointFilter<ObserveResourceFilter>]
internal sealed class OperationOutcomeGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/sample/outcomes").WithTags("OperationOutcomes");
    }
}

internal sealed class CreateOutcomeCustomerEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/customers";

    [EndpointResponse(400)]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(409)]
    [EndpointFilter<CustomerCreateOutcomeFilter>]
    public async Task<Results<Created<CustomerOutcomeDto>, ForbidHttpResult, ProblemHttpResult>> HandleAsync([FromBody] OperationOutcomeCommand request, OperationOutcomeService operations, HttpContext context, CancellationToken cancellationToken)
    {
        var outcome = await operations.ExecuteAsync(request.Outcome, cancellationToken);
        OperationOutcomeProblems.MarkReplay(context, outcome.Replayed);
        return TypedResults.Created($"/sample/outcomes/customers/{outcome.Id}", new CustomerOutcomeDto(outcome.Id));
    }
}

internal sealed class CreateOutcomeProjectEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/projects";

    [EndpointResponse(201, contentType: "application/json")]
    [EndpointResponse(409, typeof(ProblemDetails), "application/problem+json")]
    [EndpointFilter<ExistingRequestOutcomeFilter>]
    [EndpointFilter<ReplayConflictOutcomeFilter>]
    public async Task<IResult> HandleAsync([FromBody] OperationOutcomeCommand request, OperationOutcomeService operations, HttpContext context, CancellationToken cancellationToken)
    {
        var outcome = await operations.ExecuteAsync(request.Outcome, cancellationToken);
        OperationOutcomeProblems.MarkReplay(context, outcome.Replayed);
        return Results.Created($"/sample/outcomes/projects/{outcome.Id}", new { id = outcome.Id });
    }
}

internal sealed class QueueOutcomeReportEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/reports";

    [EndpointResponse(200)]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(404, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(409, typeof(ProblemDetails), "application/problem+json")]
    [ResponseHeader("CacheControl", "no-store", ResultType = typeof(Ok<>))]
    [EndpointFilter<ExistingCommandOutcomeFilter>]
    [EndpointFilter<ReplayConflictOutcomeFilter>]
    public async Task<IResult> HandleAsync([FromBody] OperationOutcomeCommand request, OperationOutcomeService operations, HttpContext context, CancellationToken cancellationToken)
    {
        var outcome = await operations.ExecuteAsync(request.Outcome, cancellationToken);
        OperationOutcomeProblems.MarkReplay(context, outcome.Replayed);
        return Results.Ok(new { ReportId = outcome.Id, AnalysisRunId = 5L, ProjectVersionId = 3L });
    }
}

internal sealed class CancelOutcomeOperationEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/operations/{resourceId:int}/cancel";

    [EndpointResponse(409)]
    [EndpointFilter<ExistingReadOutcomeFilter>]
    [EndpointFilter<CompletedOperationOutcomeFilter>]
    public async Task<Results<NoContent, NotFound, ProblemHttpResult>> HandleAsync([FromRoute] int resourceId, [FromBody] OperationOutcomeCommand request, OperationOutcomeService operations, CancellationToken cancellationToken)
    {
        if (!await operations.OwnsAsync(request.Outcome, cancellationToken)) { return TypedResults.NotFound(); }

        await operations.ExecuteAsync(request.Outcome, cancellationToken);
        return TypedResults.NoContent();
    }
}

internal sealed class UpsertOutcomeCatalogEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/catalog";

    [EndpointResponse(409, contentType: "application/json")]
    [EndpointResponse(200, contentType: "application/json")]
    [EndpointResponse(201, contentType: "application/json")]
    [ResponseHeader("CacheControl", "no-store", ResultTypes = new[] { typeof(Ok<>), typeof(Created<>) })]
    [EndpointFilter<ExistingRequestOutcomeFilter>]
    [EndpointFilter<StaleCatalogOutcomeFilter>]
    public async Task<IResult> HandleAsync([FromBody] OperationOutcomeCommand request, OperationOutcomeService operations, CancellationToken cancellationToken)
    {
        var outcome = await operations.ExecuteAsync(request.Outcome, cancellationToken);
        return request.Update ? Results.Ok(new { id = outcome.Id, revision = 3L }) : Results.Created($"/sample/outcomes/catalog/{outcome.Id}", new { id = outcome.Id, revision = 3L });
    }
}

internal sealed class QueueOutcomeIntakeEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/intake/{resourceId:int}";

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointFilter<ExistingReadOutcomeFilter>]
    [EndpointFilter<IntakeValidationOutcomeFilter>]
    public async Task<Results<Accepted<GenerationOutcomeDto>, NotFound, ProblemHttpResult>> HandleAsync([FromRoute] int resourceId, [FromBody] OperationOutcomeCommand request, OperationOutcomeService operations, CancellationToken cancellationToken)
    {
        if (!await operations.OwnsAsync(request.Outcome, cancellationToken)) { return TypedResults.NotFound(); }

        var outcome = await operations.ExecuteAsync(request.Outcome, cancellationToken);
        return TypedResults.Accepted($"/sample/outcomes/intake/{resourceId}/generation", new GenerationOutcomeDto(outcome.Id, "Queued"));
    }
}