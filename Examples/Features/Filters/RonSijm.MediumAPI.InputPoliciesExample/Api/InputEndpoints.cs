using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.InputPoliciesExample.Api;

[EndpointFilter<IngressFilter>]
[ResponseHeader("X-Input-Group", "enabled")]
[EndpointFilter<ObserveInputFilter>]
public sealed class InputGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/sample").WithTags("InputPolicies");
    }
}

[EndpointFilter<TypedCommandFilter>]
public sealed class InviteMemberEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/members/{workspaceId:int}/invitations";

    [EndpointResponse(400)]
    [EndpointResponse(403)]
    [EndpointResponse(409)]
    public async Task<Results<Results<Created<MembershipReceipt>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromRoute] int workspaceId, [FromBody] InputRequest request, [FromServices] InputOperations operations, [FromServices] InputTrace trace, CancellationToken cancellationToken)
    {
        Results<Created<MembershipReceipt>, ProblemHttpResult> inner = await MembershipPolicy.RunInnerAsync(async () => TypedResults.Created("/members/" + workspaceId, await operations.InviteAsync(workspaceId, request, cancellationToken)), trace);
        return inner;
    }
}

[EndpointFilter<FieldInputFilter>]
public sealed class StartIntakeEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/intake/{workspaceId:int}";

    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(413, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    public async Task<Results<Results<Accepted<IntakeReceipt>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromRoute] int workspaceId, [FromBody] InputRequest request, [FromServices] InputOperations operations, CancellationToken cancellationToken)
    {
        return await IntakeApplication.StartAsync(workspaceId, request, operations, cancellationToken);
    }
}

public abstract class ReportHandler
{
    [EndpointResponse(200, contentType: SyntheticReport.MediaType)]
    [EndpointResponse(400, typeof(ProblemDetails), "application/problem+json")]
    [EndpointResponse(401)]
    [EndpointResponse(403)]
    [ResponseHeader("CacheControl", "no-store")]
    public async Task<Results<FileContentHttpResult, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromRoute] int workspaceId, [FromQuery] string? outcome, [FromQuery] Guid? receiptId, [FromServices] InputOperations operations, CancellationToken cancellationToken)
    {
        return TypedResults.File(await operations.ReportAsync(workspaceId, outcome, receiptId, cancellationToken), SyntheticReport.MediaType, SyntheticReport.DownloadName);
    }
}

[EndpointFilter<FieldInputFilter>]
public sealed class DownloadReportEndpoint : ReportHandler, IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/intake/{workspaceId:int}/report";
}