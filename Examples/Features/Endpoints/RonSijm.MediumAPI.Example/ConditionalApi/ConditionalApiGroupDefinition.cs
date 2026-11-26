using System.Diagnostics.CodeAnalysis;

namespace RonSijm.MediumAPI.Example.ConditionalApi;

public sealed class ConditionalApiGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        context.Services.GetService<ConditionalMappingProbe>()?.RecordRootCondition();
        return context.Environment.IsEnvironment("Enabled") && context.Configuration.GetValue<bool>("ConditionTests:Enabled");
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.ServiceProvider.GetRequiredService<ConditionalMappingProbe>().RecordRootMap();
        var group = endpoints.MapGroup("/conditional").WithTags("Conditional API");
        group.MapHealthChecks("/native");
        return group;
    }
}

internal sealed class ConditionalValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value";

    public string HandleAsync() => "conditional";
    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithName("ConditionalValue");
}

public sealed class ConditionalMappingProbe
{
    public int RootConditions { get; private set; }
    public int ChildConditions { get; private set; }
    public int RootMaps { get; private set; }
    public int ChildMaps { get; private set; }
    public void RecordRootCondition() => RootConditions++;
    public void RecordChildCondition() => ChildConditions++;
    public void RecordRootMap() => RootMaps++;
    public void RecordChildMap() => ChildMaps++;
}