using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResponseHeadersExample.Endpoints.Multiple;

public sealed record MultipleResultReply(string Kind);

[ResponseHeader("X-Multiple-Before", "before")]
[ResponseHeader("X-Multiple-Group", "group", ResultTypes = new[] { typeof(Ok<>), typeof(Created<>) })]
public sealed class MultipleResultsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/multiple-results");
        var declaration = new ResponseHeaderAttribute("X-Multiple-Match", "once") { ResultTypes = [typeof(IResult), typeof(Ok<>), typeof(Ok<MultipleResultReply>)] };
        group.MapGet("/fluent", MultipleResultBodies.Fluent).WithName("MultipleNativeFluent").WithResponseHeader(declaration);
        return group;
    }
}

[ResponseHeader("X-Multiple-Filter", "filter", ResultTypes = new[] { typeof(Ok<>), typeof(Created<>) })]
public sealed class MultipleResultFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.GetArgument<bool?>(1) == true)
        {
            return ValueTask.FromResult<object?>(TypedResults.Created("/multiple-results/items/blocked", new MultipleResultReply("blocked")));
        }

        return next(context);
    }
}

[ResponseHeader("X-Multiple-Class", "class", ResultTypes = new[] { typeof(Ok<>), typeof(Created<>) })]
[EndpointFilter<MultipleResultFilter>]
public sealed class UpsertMultipleResultsEndpoint : IPutEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/upsert/{kind}";

    [ResponseHeader("CacheControl", "no-store", ResultTypes = new[] { typeof(Ok<>), typeof(Created<>) })]
    public Results<Results<Ok<MultipleResultReply>, Created<MultipleResultReply>>, ProblemHttpResult> HandleAsync([FromRoute] string kind, [FromQuery] bool? block)
    {
        return MultipleResultBodies.Upsert(kind);
    }
}

public sealed class StartedMultipleResultsEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/started";

    [ResponseHeader("X-Multiple-Started", "late", ResultTypes = new[] { typeof(StartedReplyResult), typeof(Ok<>) })]
    public Task<StartedReplyResult> HandleAsync(HttpContext context)
    {
        return MultipleResultBodies.Started(context);
    }
}

public sealed class StartedReplyResult : IResult
{
    public Task ExecuteAsync(HttpContext context)
    {
        return context.Response.WriteAsync("complete");
    }
}

public static class MultipleResultBodies
{
    public static Results<Results<Ok<MultipleResultReply>, Created<MultipleResultReply>>, ProblemHttpResult> Upsert(string kind)
    {
        if (kind == "fault")
        {
            throw new InvalidOperationException("Synthetic result-selection failure.");
        }

        if (kind == "cancel")
        {
            throw new OperationCanceledException("Synthetic result-selection cancellation.");
        }

        if (kind == "ok")
        {
            Results<Ok<MultipleResultReply>, Created<MultipleResultReply>> success = TypedResults.Ok(new MultipleResultReply("ok"));
            return success;
        }

        if (kind == "created")
        {
            Results<Ok<MultipleResultReply>, Created<MultipleResultReply>> success = TypedResults.Created("/multiple-results/items/7", new MultipleResultReply("created"));
            return success;
        }

        return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Choose ok or created.", type: "urn:sample:upsert-kind");
    }

    public static Ok<MultipleResultReply> Fluent()
    {
        return TypedResults.Ok(new MultipleResultReply("fluent"));
    }

    public static async Task<StartedReplyResult> Started(HttpContext context)
    {
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("started:");
        return new StartedReplyResult();
    }
}