using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.InputPoliciesExample;

public static class LegacyInputs
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/sample").WithTags("InputPolicies").AddEndpointFilter<IngressFilter>();
        group.AddEndpointFilter((context, next) =>
        {
            context.HttpContext.Response.Headers["X-Input-Group"] = "enabled";
            return next(context);
        });
        group.AddEndpointFilter<ObserveInputFilter>();
        var membership = MapOriginal(group, "/members/{workspaceId:int}/invitations", Invite).WithName("InviteMember");
        membership.Add(builder =>
        {
            if (!builder.Metadata.OfType<IProducesResponseTypeMetadata>().Any(response => response.StatusCode == 404))
            {
                builder.Metadata.Add(new ProducesResponseTypeMetadata(404, typeof(void), []));
            }
        });
        MapOriginal(group, "/intake/{workspaceId:int}", Intake).WithName("StartIntake");
        MapOriginal(group, "/intake/{workspaceId:int}/report", Report, get: true).WithName("DownloadReport");
    }

    [EndpointResponse(400)]
    [EndpointResponse(403)]
    [EndpointResponse(409)]
    public static Task<Results<Results<Created<MembershipReceipt>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> Invite([FromRoute] int workspaceId, [FromBody] InputRequest request, [FromServices] InputOperations operations, [FromServices] InputTrace trace, CancellationToken cancellationToken)
    {
        return CommonCommandPolicy.RunAsync<Results<Created<MembershipReceipt>, ProblemHttpResult>>(() => MembershipPolicy.RunInnerAsync(async () => TypedResults.Created("/members/" + workspaceId, await operations.InviteAsync(workspaceId, request, cancellationToken)), trace), trace);
    }

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(413, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public static Task<Results<Results<Accepted<IntakeReceipt>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> Intake([FromRoute] int workspaceId, [FromBody] InputRequest request, [FromServices] InputOperations operations, [FromServices] InputTrace trace, CancellationToken cancellationToken)
    {
        return RunInputAsync<Results<Accepted<IntakeReceipt>, ProblemHttpResult>>(() => IntakeApplication.StartAsync(workspaceId, request, operations, cancellationToken), trace);
    }

    [EndpointResponse(200, contentType: SyntheticReport.MediaType)]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public static Task<Results<FileContentHttpResult, ForbidHttpResult, NotFound, ProblemHttpResult>> Report([FromRoute] int workspaceId, [FromQuery] string? outcome, [FromQuery] Guid? receiptId, [FromServices] InputOperations operations, [FromServices] InputTrace trace, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return RunInputAsync<FileContentHttpResult>(async () => TypedResults.File(await operations.ReportAsync(workspaceId, outcome, receiptId, cancellationToken), SyntheticReport.MediaType, SyntheticReport.DownloadName), trace);
    }

    private static async Task<Results<TSuccess, ForbidHttpResult, NotFound, ProblemHttpResult>> RunInputAsync<TSuccess>(Func<Task<TSuccess>> operation, InputTrace trace) where TSuccess : IResult
    {
        trace.Entries.Add("input:enter");
        try
        {
            return await CommonCommandPolicy.RunAsync<TSuccess>(operation, trace);
        }
        catch (FieldInputException exception)
        {
            trace.Entries.Add("input:handled");
            return InputProblems.Input(exception.Code, exception.Field);
        }
        finally
        {
            trace.Entries.Add("input:exit");
        }
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler, bool get = false)
    {
        return get ? endpoints.MapGet(pattern, handler) : endpoints.MapPost(pattern, handler);
    }
}