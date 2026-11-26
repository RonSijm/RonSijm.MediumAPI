using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.SharedResponsesExample;

[EndpointResponse(StatusCodes.Status400BadRequest, typeof(FilterError))]
[ResponseHeader("X-Filter-Owner", "negative")]
public sealed class RejectNegativeFilter : IEndpointFilter
{
    // A similarly named method is not a convention opt-in on a plain native filter.
    public static void Configure(IEndpointConventionBuilder endpoint) => throw new InvalidOperationException("A plain filter's similarly named method must not run.");

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.RequestServices.GetRequiredService<SharedResponseProbe>().VisitGuard();
        return context.GetArgument<int>(0) < 0
            ? ValueTask.FromResult<object?>(TypedResults.BadRequest(new FilterError("negative", "Use a nonnegative value.")))
            : next(context);
    }
}

[EndpointResponse(StatusCodes.Status409Conflict, typeof(ProblemDetails), SharedResponseMetadata.ProblemJson)]
public sealed class SpecialValueFilter : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint) => endpoint.WithMetadata(new EndpointResponseAttribute(StatusCodes.Status429TooManyRequests, typeof(FilterError)));

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.RequestServices.GetRequiredService<SharedResponseProbe>().VisitConvention();
        return context.GetArgument<int>(0) == 13
            ? ValueTask.FromResult<object?>(TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Choose another value.", type: "urn:sample:special-value"))
            : next(context);
    }
}