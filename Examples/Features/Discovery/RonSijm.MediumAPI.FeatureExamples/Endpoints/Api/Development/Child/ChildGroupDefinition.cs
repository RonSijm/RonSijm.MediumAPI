namespace RonSijm.MediumAPI.FeatureExamples.Endpoints.Api.Development.Child;

public sealed class ChildGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        context.Services.GetRequiredService<FeatureMappingProbe>().ChildConditions++;
        return context.Configuration.GetValue<bool>("Features:Child");
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.ServiceProvider.GetRequiredService<FeatureMappingProbe>().ChildMaps++;
        return endpoints.MapGroup("/child");
    }
}