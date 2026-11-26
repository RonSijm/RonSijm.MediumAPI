namespace RonSijm.MediumAPI.CustomMetadataExample;

public sealed class ReadMetadataFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.Response.Headers["X-Sample-Metadata"] = string.Join(",", MetadataReplies.Labels(context.HttpContext));
        return next(context);
    }
}

[AppMetadata("filter")]
[SampleAccess(Policy = "SampleReaders")]
[CustomResponse]
[EndpointResponseFallback(StatusCodes.Status404NotFound)]
[UnmarkedThrowing]
public sealed class ConfiguredMetadataFilter : IMediumApiEndpointFilter
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.WithMetadata(new AppMetadataAttribute("filter-configure"));
    }

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return next(context);
    }
}

[AppMetadata("ignored-filter-base")]
public abstract class MetadataFilterBase : IEndpointFilter
{
    [AppMetadata("ignored-filter-method")]
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        return next(context);
    }
}

[AppMetadata("derived-filter")]
[UnmarkedThrowing]
public sealed class DerivedMetadataFilter : MetadataFilterBase { }