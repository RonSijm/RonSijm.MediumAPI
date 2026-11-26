namespace RonSijm.MediumAPI.ExceptionFiltersExample.Endpoints.Disabled;

[EndpointFilter<MustStayDisabledFilter>]
public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context) => false;

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/disabled");
        group.MapGet("/native", () => TypedResults.Ok("disabled"));
        return group;
    }
}