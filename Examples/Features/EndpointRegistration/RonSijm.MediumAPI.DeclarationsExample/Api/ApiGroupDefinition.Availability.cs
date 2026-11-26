namespace RonSijm.MediumAPI.DeclarationsExample.Api;

internal sealed partial class ApiGroupDefinition
{
    public static bool ShouldMap(EndpointMappingContext context) => context.Services.GetRequiredService<DeclarationOptions>().EnableApi;
}