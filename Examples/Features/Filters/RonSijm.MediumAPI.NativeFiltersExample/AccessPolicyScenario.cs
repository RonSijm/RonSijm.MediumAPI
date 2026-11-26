using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;

namespace RonSijm.MediumAPI.NativeFiltersExample;

public sealed class WorkspaceAccessConvention : IMediumApiEndpointConvention
{
    private WorkspaceAccessConvention()
    {
        throw new InvalidOperationException("A startup convention must not be instantiated.");
    }

    public static void Configure(IEndpointConventionBuilder builder)
    {
        builder.WithMetadata(new AuthorizeAttribute { Policy = BrowserPolicies.Workspace }, new EnableCorsAttribute(BrowserPolicies.Cors));
    }
}

public static class AccessPolicyScenario
{
    public static void Map(WebApplication app)
    {
        var group = app.TryMapMediumApiNamespace<AccessPolicyEndpoints.AccessPolicyGroupDefinition>();
        if (group is not null) { MapEntries(group); }
    }

    public static void MapEntries(RouteGroupBuilder group, bool suppressNames = false)
    {
        var read = group.MapMediumApiEndpoint<AccessPolicyEndpoints.AccessReadEndpoint>();
        var value = group.MapMediumApiEndpoint<AccessPolicyEndpoints.AccessValueEndpoint>();
        if (suppressNames)
        {
            read.WithoutDefaultEndpointName();
            value.WithoutDefaultEndpointName();
        }

        group.MapMediumApiEndpoint<AccessPolicyEndpoints.AccessValueEndpoint>("/value-alias/{value:int}").WithoutDefaultEndpointName();
    }
}