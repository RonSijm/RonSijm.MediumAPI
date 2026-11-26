namespace RonSijm.MediumAPI.ProviderPoliciesExample;

[EndpointResponseFallback(404)]
public sealed class TypedCommandPolicyFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return InvokeCommandAsync(context, next);
    }

    public static async ValueTask<object?> InvokeCommandAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var trace = context.HttpContext.RequestServices.GetRequiredService<PolicyTrace>();
        var result = await CommandPolicy.RunAsync<IResult>(async () => (IResult)(await next(context))!, trace);
        return result.Result;
    }
}

public sealed class LocalTypedProviderPolicyFilter(PolicyTrace trace) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        trace.Entries.Add("provider:enter");
        try
        {
            return await TypedCommandPolicyFilter.InvokeCommandAsync(context, next);
        }
        catch (LocalProviderException)
        {
            trace.Entries.Add("provider:handled");
            return ProviderProblems.Local();
        }
        catch (ProviderConfigurationException)
        {
            trace.Entries.Add("provider:configuration");
            return ProviderProblems.Local();
        }
        finally
        {
            trace.Entries.Add("provider:exit");
        }
    }
}

// Deliberately no response declarations: invoking a static mapper imports no filter attributes.
public sealed class LocalDynamicProviderPolicyFilter(PolicyTrace trace) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return TypedCommandPolicyFilter.InvokeCommandAsync(context, invocation => InvokeProviderAsync(invocation, next));
    }

    private async ValueTask<object?> InvokeProviderAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        trace.Entries.Add("provider:enter");
        try
        {
            return await next(context);
        }
        catch (LocalProviderException)
        {
            trace.Entries.Add("provider:handled");
            return ProviderProblems.Local();
        }
        catch (ProviderConfigurationException)
        {
            trace.Entries.Add("provider:configuration");
            return ProviderProblems.Local();
        }
        finally
        {
            trace.Entries.Add("provider:exit");
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

public sealed class ObservePolicyFilter(PolicyTrace trace, ProviderOperations operations) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);
        context.HttpContext.Response.Headers["X-Policy-Trace"] = string.Join(",", trace.Entries);
        context.HttpContext.Response.Headers["X-Service-Scope"] = operations.ScopeId.ToString("D");
        context.HttpContext.Response.Headers["X-Filter-Scope"] = trace.ScopeId.ToString("D");
        return result;
    }
}