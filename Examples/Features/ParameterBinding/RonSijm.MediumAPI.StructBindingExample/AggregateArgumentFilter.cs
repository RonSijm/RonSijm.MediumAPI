namespace RonSijm.MediumAPI.StructBindingExample;

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

    public static bool MatchesBoundValue(HttpContext context, object request) => context.Items[ArgumentKey] is { } value && value.GetType() == request.GetType() && value.Equals(request);
    public static bool MatchesService(HttpContext context, AggregateService service) => context.Items[ArgumentKey] switch { LocalItemRequest request => ReferenceEquals(request.Service, service), WorkspaceItemRequest request => ReferenceEquals(request.Service, service), _ => false };
    public static int GetArgumentCount(HttpContext context) => context.Items[ArgumentCountKey] is int count ? count : -1;
}