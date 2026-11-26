namespace RonSijm.MediumAPI.NativeFiltersExample;

[ResponseHeader("X-Native-Gate", "present")]
public sealed class HeaderGateFilter(RequestTrace trace) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        trace.Entries.Add("gate:enter");
        try
        {
            if (context.HttpContext.Request.Headers["X-Sample-Block"] == "true")
            {
                return TypedResults.BadRequest(new NativeFilterError("blocked"));
            }

            return await next(context);
        }
        finally
        {
            trace.Entries.Add("gate:exit");
        }
    }
}