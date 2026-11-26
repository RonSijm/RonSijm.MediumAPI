namespace RonSijm.MediumAPI.FeatureExamples.Endpoints.Api.Development;

public sealed class DevelopmentGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        context.Services.GetRequiredService<FeatureMappingProbe>().DevelopmentConditions++;
        return context.Environment.IsDevelopment() && context.Configuration.GetValue<bool>("Features:Development");
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.ServiceProvider.GetRequiredService<FeatureMappingProbe>().DevelopmentMaps++;
        var group = endpoints.MapGroup("/development");
        group.MapHealthChecks("/health");
        return group;
    }
}