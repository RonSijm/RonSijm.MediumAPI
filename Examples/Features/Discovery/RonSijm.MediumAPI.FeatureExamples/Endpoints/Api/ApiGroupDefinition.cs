namespace RonSijm.MediumAPI.FeatureExamples.Endpoints.Api;

public sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/api").WithTags("Feature examples");
    }
}