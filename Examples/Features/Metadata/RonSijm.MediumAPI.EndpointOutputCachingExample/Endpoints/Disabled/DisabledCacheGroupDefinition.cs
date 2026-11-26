using Microsoft.AspNetCore.OutputCaching;

namespace RonSijm.MediumAPI.EndpointOutputCachingExample.Endpoints.Disabled;

[OutputCache(NoStore = true)]
public sealed class DisabledCacheGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/disabled");
}