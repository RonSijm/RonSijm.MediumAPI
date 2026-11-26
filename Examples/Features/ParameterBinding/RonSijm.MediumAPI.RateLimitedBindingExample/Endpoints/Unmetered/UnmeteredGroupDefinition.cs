namespace RonSijm.MediumAPI.RateLimitedBindingExample.Endpoints.Unmetered;

public sealed class UnmeteredGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => RateLimiterEndpointConventionBuilderExtensions.DisableRateLimiting<RouteGroupBuilder>(builder: endpoints.MapGroup("/unmetered/{customerId:long}"));
}