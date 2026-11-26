using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ScopedCommandsExample;

[EndpointResponseFallback(400, typeof(ProblemDetails), "application/problem+json")]
[EndpointResponseFallback(403)]
[EndpointResponseFallback(404)]
[EndpointResponseFallback(409, typeof(ProblemDetails), "application/problem+json")]
public sealed class CommandExceptionFilter<TSuccess>(CommandTrace trace) : IEndpointFilter where TSuccess : IResult
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        trace.Entries.Add("common:enter");
        try
        {
            return await next(context);
        }
        catch (AccessDeniedException)
        {
            Results<TSuccess, ForbidHttpResult, NotFound, ProblemHttpResult> result = TypedResults.Forbid();
            return result;
        }
        catch (ResourceMissingException)
        {
            Results<TSuccess, ForbidHttpResult, NotFound, ProblemHttpResult> result = TypedResults.NotFound();
            return result;
        }
        catch (CommandConflictException)
        {
            Results<TSuccess, ForbidHttpResult, NotFound, ProblemHttpResult> result = CommandProblems.Conflict();
            return result;
        }
        catch (CommandValidationException exception)
        {
            Results<TSuccess, ForbidHttpResult, NotFound, ProblemHttpResult> result = CommandProblems.Validation(exception);
            return result;
        }
        catch (CommandDomainException)
        {
            Results<TSuccess, ForbidHttpResult, NotFound, ProblemHttpResult> result = CommandProblems.Problem(400, "invalid-request", "The request could not be completed.");
            return result;
        }
        finally
        {
            trace.Entries.Add("common:exit");
        }
    }
}

[EndpointResponseFallback(409, typeof(ProblemDetails), "application/problem+json")]
public sealed class ConfirmationPolicyFilter<TSuccess>(CommandTrace trace) : IEndpointFilter where TSuccess : IResult
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        trace.Entries.Add("confirmation:enter");
        try
        {
            return await next(context);
        }
        catch (ConfirmationRequiredException)
        {
            Results<TSuccess, ForbidHttpResult, NotFound, ProblemHttpResult> result = CommandProblems.Confirmation();
            return result;
        }
        finally
        {
            trace.Entries.Add("confirmation:exit");
        }
    }
}

public sealed class IngressFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.Request.Headers["X-Sample-Block"] == "true")
        {
            return ValueTask.FromResult<object?>(TypedResults.StatusCode(StatusCodes.Status403Forbidden));
        }

        return next(context);
    }
}

public sealed class ObserveCommandFilter(CommandTrace trace, ResourceCommands commands) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        var depth = 0;
        var terminal = result;
        while (terminal is INestedHttpResult nested)
        {
            depth++;
            terminal = nested.Result;
        }

        context.HttpContext.Response.Headers["X-Result-Depth"] = depth.ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.HttpContext.Response.Headers["X-Result-Leaf"] = terminal?.GetType().Name;
        context.HttpContext.Response.Headers["X-Command-Trace"] = string.Join(",", trace.Entries);
        context.HttpContext.Response.Headers["X-Service-Scope"] = commands.ScopeId.ToString("D");
        context.HttpContext.Response.Headers["X-Filter-Scope"] = trace.ScopeId.ToString("D");
        return result;
    }
}