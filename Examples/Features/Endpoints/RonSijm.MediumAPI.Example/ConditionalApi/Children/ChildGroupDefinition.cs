using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.Example.ConditionalApi.Children;

public sealed class ChildGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        context.Services.GetService<ConditionalMappingProbe>()?.RecordChildCondition();
        return context.Configuration.GetValue("ConditionTests:ChildEnabled", true);
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.ServiceProvider.GetRequiredService<ConditionalMappingProbe>().RecordChildMap();
        return endpoints.MapGroup("/child");
    }
}

internal sealed class ConditionalChildEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public string HandleAsync() => "child";
    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithName("ConditionalChild");
}