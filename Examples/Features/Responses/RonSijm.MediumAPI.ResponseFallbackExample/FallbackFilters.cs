namespace RonSijm.MediumAPI.ResponseFallbackExample;

[EndpointResponseFallback(StatusCodes.Status404NotFound)]
public sealed class MissingItemFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.RequestServices.GetRequiredService<FallbackProbe>().VisitFilter();
        if (context.GetArgument<bool?>(0) == true)
        {
            return ValueTask.FromResult<object?>(TypedResults.NotFound());
        }

        return next(context);
    }
}

[EndpointResponse(StatusCodes.Status404NotFound, typeof(FilterFailure))]
[EndpointResponseFallback(StatusCodes.Status404NotFound, typeof(SecondFailure), "text/plain")]
public sealed class ExplicitItemFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.RequestServices.GetRequiredService<FallbackProbe>().VisitFilter();
        if (context.GetArgument<bool?>(0) == true)
        {
            return ValueTask.FromResult<object?>(TypedResults.NotFound(new FilterFailure("filtered-item")));
        }

        return next(context);
    }
}

[EndpointResponseFallback(StatusCodes.Status401Unauthorized)]
public abstract class InheritedFilterBase : IEndpointFilter
{
    [EndpointResponseFallback(StatusCodes.Status400BadRequest)]
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return next(context);
    }
}

[EndpointResponseFallback(StatusCodes.Status403Forbidden)]
[UnrelatedFallback]
public sealed class InheritedFilter : InheritedFilterBase { }