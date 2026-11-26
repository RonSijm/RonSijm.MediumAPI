using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ScopedCommandsExample;

public static class LegacyCommands
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/commands").WithTags("ScopedCommands").AddEndpointFilter<IngressFilter>().AddEndpointFilter<ObserveCommandFilter>();
        AddCommonResponses(MapOriginal(group, "/projects/{projectId:int}/resources/{resourceId:int}", Read).WithName("ReadScopedResource"));
        AddCommonResponses(MapOriginal(group, "/projects/{projectId:int}/resources/{resourceId:int}/review", Review, post: true).WithName("ReviewScopedResource"));
        var release = MapOriginal(group, "/releases/{releaseId:int}", Release, post: true).WithName("ConfirmRelease");
        AddFallback(release, 409, typeof(ProblemDetails), ["application/problem+json"]);
        AddCommonResponses(release);
    }

    [ProducesResponseType(401)]
    public static Task<Results<Results<Ok<ResourceReceipt>, NotFound>, ForbidHttpResult, NotFound, ProblemHttpResult>> Read([FromRoute] int projectId, [FromRoute] int resourceId, [FromQuery] string? scope, [FromQuery] string? operation, [FromServices] ResourceCommands commands, [FromServices] CommandTrace trace, HttpContext context, CancellationToken cancellationToken)
    {
        NoStore(context);
        return RunAsync<Results<Ok<ResourceReceipt>, NotFound>>(() => ScopedOperations.InProjectAsync(commands, projectId, resourceId, scope, cancellationToken, () => commands.ReadAsync(resourceId, operation, cancellationToken)), trace);
    }

    [ProducesResponseType(401)]
    public static Task<Results<Results<Results<ProblemHttpResult, NoContent>, NotFound>, ForbidHttpResult, NotFound, ProblemHttpResult>> Review([FromRoute] int projectId, [FromRoute] int resourceId, [FromQuery] string? scope, [FromBody] CommandRequest request, [FromServices] ResourceCommands commands, [FromServices] CommandTrace trace, HttpContext context, CancellationToken cancellationToken)
    {
        NoStore(context);
        return RunAsync<Results<Results<ProblemHttpResult, NoContent>, NotFound>>(() => ScopedOperations.InProjectAsync(commands, projectId, resourceId, scope, cancellationToken, () => commands.ReviewAsync(request, cancellationToken)), trace);
    }

    [ProducesResponseType(401)]
    public static async Task<Results<Ok<ResourceReceipt>, ForbidHttpResult, NotFound, ProblemHttpResult>> Release([FromRoute] int releaseId, [FromBody] CommandRequest request, [FromServices] ResourceCommands commands, [FromServices] CommandTrace trace, HttpContext context, CancellationToken cancellationToken)
    {
        NoStore(context);
        trace.Entries.Add("confirmation:enter");
        try
        {
            return await RunAsync<Ok<ResourceReceipt>>(() => commands.ReleaseAsync(releaseId, request, cancellationToken), trace);
        }
        catch (ConfirmationRequiredException)
        {
            return CommandProblems.Confirmation();
        }
        finally
        {
            trace.Entries.Add("confirmation:exit");
        }
    }

    private static async Task<Results<TSuccess, ForbidHttpResult, NotFound, ProblemHttpResult>> RunAsync<TSuccess>(Func<Task<TSuccess>> command, CommandTrace trace) where TSuccess : IResult
    {
        trace.Entries.Add("common:enter");
        try
        {
            return await command();
        }
        catch (AccessDeniedException)
        {
            return TypedResults.Forbid();
        }
        catch (ResourceMissingException)
        {
            return TypedResults.NotFound();
        }
        catch (CommandConflictException)
        {
            return CommandProblems.Conflict();
        }
        catch (CommandValidationException exception)
        {
            return CommandProblems.Validation(exception);
        }
        catch (CommandDomainException)
        {
            return CommandProblems.Problem(400, "invalid-request", "The request could not be completed.");
        }
        finally
        {
            trace.Entries.Add("common:exit");
        }
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler, bool post = false)
    {
        return post ? endpoints.MapPost(pattern, handler) : endpoints.MapGet(pattern, handler);
    }

    private static void NoStore(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
    }

    private static void AddCommonResponses(RouteHandlerBuilder endpoint)
    {
        AddFallback(endpoint, 400, typeof(ProblemDetails), ["application/problem+json"]);
        AddFallback(endpoint, 403, typeof(void), []);
        AddFallback(endpoint, 404, typeof(void), []);
        AddFallback(endpoint, 409, typeof(ProblemDetails), ["application/problem+json"]);
    }

    private static void AddFallback(RouteHandlerBuilder endpoint, int status, Type responseType, string[] contentTypes)
    {
        endpoint.Add(builder =>
        {
            if (!builder.Metadata.OfType<IProducesResponseTypeMetadata>().Any(metadata => metadata.StatusCode == status))
            {
                builder.Metadata.Add(new ProducesResponseTypeMetadata(status, responseType, contentTypes));
            }
        });
    }
}