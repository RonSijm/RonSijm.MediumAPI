using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace RonSijm.MediumAPI.Example.IntegrationTest.Unselected;

internal sealed class UnselectedGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context) => context.Configuration.GetValue<bool>("ConditionTests:ThrowUnselected")
        ? throw new InvalidOperationException("The selected assembly's condition failed.")
        : false;

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("This test group never maps.");
}