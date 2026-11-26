namespace RonSijm.MediumAPI.DeclarationsExample.Api.Optional;

internal sealed class OptionalGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context) => context.Services.GetRequiredService<DeclarationOptions>().EnableOptional;

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/optional");
}