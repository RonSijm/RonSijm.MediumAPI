using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.NamespaceMetadataExample;

public static class DeclarativeGroupMetadata
{
    public const string RootTag = "Declarative root";
    public const string EmptyTag = "Empty namespace";
    public const string ItemTag = "Declarative items";
    public const string AliasTag = "Declarative alias";
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class DeclarativeStageAttribute : Attribute, IEndpointMetadataAttribute
{
    public DeclarativeStageAttribute(string stage)
    {
        Stage = stage;
    }

    public string Stage { get; }
}

public sealed class DeclarativeMappingProbe
{
    public int RootConditions { get; set; }
    public int EmptyConditions { get; set; }
    public int DisabledConditions { get; set; }
    public int Materializations { get; set; }
    public int EndpointActivations { get; set; }
    public int Calls { get; set; }
}

public sealed record DeclarativeReply(long TenantId, int? ItemId, string Source);

public readonly struct DeclarativeObservationConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        builder.Add(endpoint => endpoint.ApplicationServices.GetRequiredService<DeclarativeMappingProbe>().Materializations++);
    }
}

public static class DeclarativeGroupsScenario
{
    public static void Map(WebApplication app)
    {
        var group = app.TryMapMediumApiNamespace<DeclarativeRoutes.DeclarativeRootGroupDefinition>(ConfigureMount);
        if (group is null) { return; }

        app.MapMediumApiEndpoint<DeclarativeRoutes.GetDeclarativeRootEndpoint>("/declarative-alias/{tenantId:long}").WithoutDefaultEndpointName().WithTags(DeclarativeGroupMetadata.AliasTag);
    }

    public static void ConfigureMount(Microsoft.AspNetCore.Routing.RouteGroupBuilder group)
    {
        group.WithMetadata(new DeclarativeStageAttribute("host"));
        group.MapGet("/native", NativeValue).WithName("DeclarativeNative");
    }

    public static Ok<DeclarativeReply> NativeValue(long tenantId, DeclarativeMappingProbe probe)
    {
        probe.Calls++;
        return TypedResults.Ok(new DeclarativeReply(tenantId, null, "native"));
    }
}