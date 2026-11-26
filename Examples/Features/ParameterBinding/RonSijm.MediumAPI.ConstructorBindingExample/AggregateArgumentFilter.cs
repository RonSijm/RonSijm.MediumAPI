namespace RonSijm.MediumAPI.ConstructorBindingExample;

public sealed class AggregateArgumentFilter : IEndpointFilter
{
    private static readonly object ArgumentKey = new();
    private static readonly object ArgumentCountKey = new();

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.Items[ArgumentKey] = context.Arguments[0];
        context.HttpContext.Items[ArgumentCountKey] = context.Arguments.Count;
        return next(context);
    }

    public static bool IsOriginalArgument(HttpContext context, object request) => ReferenceEquals(context.Items[ArgumentKey], request);
    public static int GetArgumentCount(HttpContext context) => context.Items[ArgumentCountKey] is int count ? count : -1;
}