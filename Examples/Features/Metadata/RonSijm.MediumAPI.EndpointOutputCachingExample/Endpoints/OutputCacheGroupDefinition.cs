namespace RonSijm.MediumAPI.EndpointOutputCachingExample.Endpoints;

public sealed class OutputCacheGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/output-cache");
}