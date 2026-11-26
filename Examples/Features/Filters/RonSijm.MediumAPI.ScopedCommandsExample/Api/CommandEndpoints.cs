using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ScopedCommandsExample.Api;

[EndpointFilter<IngressFilter>]
[EndpointFilter<ObserveCommandFilter>]
public sealed class CommandsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/commands").WithTags("ScopedCommands");
    }
}

public abstract class ReadCommandHandler
{
    [ProducesResponseType(401)]
    [ResponseHeader("CacheControl", "no-store")]
    [ResponseHeader("Pragma", "no-cache")]
    public async Task<Results<Results<Ok<ResourceReceipt>, NotFound>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromRoute] int projectId, [FromRoute] int resourceId, [FromQuery] string? scope, [FromQuery] string? operation, [FromServices] ResourceCommands commands, CancellationToken cancellationToken)
    {
        Results<Ok<ResourceReceipt>, NotFound> inner = await ScopedOperations.InProjectAsync(commands, projectId, resourceId, scope, cancellationToken, () => commands.ReadAsync(resourceId, operation, cancellationToken));
        return inner;
    }
}

[EndpointFilter<CommandExceptionFilter<Results<Ok<ResourceReceipt>, NotFound>>>]
public sealed class ReadScopedResourceEndpoint : ReadCommandHandler, IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/projects/{projectId:int}/resources/{resourceId:int}";
}

[EndpointFilter<CommandExceptionFilter<Results<Results<ProblemHttpResult, NoContent>, NotFound>>>]
public sealed class ReviewScopedResourceEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/projects/{projectId:int}/resources/{resourceId:int}/review";

    [ProducesResponseType(401)]
    [ResponseHeader("CacheControl", "no-store")]
    [ResponseHeader("Pragma", "no-cache")]
    public async Task<Results<Results<Results<ProblemHttpResult, NoContent>, NotFound>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromRoute] int projectId, [FromRoute] int resourceId, [FromQuery] string? scope, [FromBody] CommandRequest request, [FromServices] ResourceCommands commands, CancellationToken cancellationToken)
    {
        Results<Results<ProblemHttpResult, NoContent>, NotFound> inner = await ScopedOperations.InProjectAsync(commands, projectId, resourceId, scope, cancellationToken, () => commands.ReviewAsync(request, cancellationToken));
        return inner;
    }
}

[EndpointFilter<ConfirmationPolicyFilter<Ok<ResourceReceipt>>>]
[EndpointFilter<CommandExceptionFilter<Ok<ResourceReceipt>>>]
public sealed class ConfirmReleaseEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/releases/{releaseId:int}";

    [ProducesResponseType(401)]
    [ResponseHeader("CacheControl", "no-store")]
    [ResponseHeader("Pragma", "no-cache")]
    public async Task<Results<Ok<ResourceReceipt>, ForbidHttpResult, NotFound, ProblemHttpResult>> HandleAsync([FromRoute] int releaseId, [FromBody] CommandRequest request, [FromServices] ResourceCommands commands, CancellationToken cancellationToken)
    {
        return await commands.ReleaseAsync(releaseId, request, cancellationToken);
    }
}