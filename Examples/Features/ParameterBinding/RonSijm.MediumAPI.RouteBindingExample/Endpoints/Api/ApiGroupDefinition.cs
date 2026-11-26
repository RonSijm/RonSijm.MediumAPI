namespace RonSijm.MediumAPI.RouteBindingExample.Endpoints.Api;

internal sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/api/{customerId:long}").WithTags("Inherited routes").WithGroupName("RouteBinding").WithSummary("Customer route bindings");
}