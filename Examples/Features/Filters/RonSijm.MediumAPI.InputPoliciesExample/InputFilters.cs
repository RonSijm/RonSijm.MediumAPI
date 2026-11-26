namespace RonSijm.MediumAPI.InputPoliciesExample;

[EndpointResponseFallback(404)]
public sealed class TypedCommandFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return InvokeCommandAsync(context, next);
    }

    public static async ValueTask<object?> InvokeCommandAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var trace = context.HttpContext.RequestServices.GetRequiredService<InputTrace>();
        var result = await CommonCommandPolicy.RunAsync<IResult>(async () => (IResult)(await next(context))!, trace);
        return result.Result;
    }
}

// Static shared-policy invocation imports no response declarations from TypedCommandFilter.
public sealed class FieldInputFilter(InputTrace trace) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        trace.Entries.Add("input:enter");
        try
        {
            return await TypedCommandFilter.InvokeCommandAsync(context, next);
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

public sealed class ObserveInputFilter(InputTrace trace, InputOperations operations) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        context.HttpContext.Response.Headers["X-Input-Trace"] = string.Join(",", trace.Entries);
        context.HttpContext.Response.Headers["X-Service-Scope"] = operations.ScopeId.ToString("D");
        context.HttpContext.Response.Headers["X-Filter-Scope"] = trace.ScopeId.ToString("D");
        return result;
    }
}