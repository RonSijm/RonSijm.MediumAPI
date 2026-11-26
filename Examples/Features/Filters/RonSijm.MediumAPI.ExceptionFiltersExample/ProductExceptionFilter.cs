using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ExceptionFiltersExample;

/// <summary>Adapts the sample's existing two-exception contract and declares only those responses.</summary>
public sealed class ProductExceptionFilter : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint) => endpoint.WithMetadata(new EndpointResponseAttribute(StatusCodes.Status404NotFound), new EndpointResponseAttribute(StatusCodes.Status409Conflict, typeof(ProblemDetails), "application/problem+json"));

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (ProductMissingException) when (!context.HttpContext.Response.HasStarted)
        {
            return TypedResults.NotFound();
        }
        catch (ProductConflictException) when (!context.HttpContext.Response.HasStarted)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "The product changed. Reload and try again.", extensions: new Dictionary<string, object?> { ["code"] = "product-conflict" });
        }
    }
}