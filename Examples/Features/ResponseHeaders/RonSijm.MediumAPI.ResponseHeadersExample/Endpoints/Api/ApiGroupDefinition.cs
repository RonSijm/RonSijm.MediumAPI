namespace RonSijm.MediumAPI.ResponseHeadersExample.Endpoints.Api;

[ResponseHeader("X-Group", "api")]
[UnrelatedHeaderExample]
public sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");
        group.MapGet("/native", () => TypedResults.Ok("native"));
        return group;
    }
}