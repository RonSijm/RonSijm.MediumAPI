using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.DeclarativeRoutes
{
    [NamespaceRouteGroup("/declarative/{tenantId:long}")]
    [Tags(DeclarativeGroupMetadata.RootTag)]
    [DeclarativeStage("root")]
    [EndpointConvention<DeclarativeObservationConvention>]
    [ResponseHeader("X-Declarative", "root")]
    public sealed partial class DeclarativeRootGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            context.Services.GetRequiredService<DeclarativeMappingProbe>().RootConditions++;
            var configuration = context.Services.GetRequiredService<IConfiguration>();
            return configuration.GetValue("Sample:DeclarativeGroups", false) && configuration.GetValue("Sample:DeclarativeEnabled", true);
        }
    }

    public sealed class GetDeclarativeRootEndpoint : IGetEndpoint
    {
        public GetDeclarativeRootEndpoint(DeclarativeMappingProbe probe)
        {
            probe.EndpointActivations++;
        }

        [StringSyntax("Route")]
        public string Pattern => "/status";

        public Ok<DeclarativeReply> HandleAsync([FromRoute] long tenantId, DeclarativeMappingProbe probe)
        {
            probe.Calls++;
            return TypedResults.Ok(new DeclarativeReply(tenantId, null, "root"));
        }
    }
}

namespace RonSijm.MediumAPI.NamespaceMetadataExample.DeclarativeRoutes.Empty
{
    [NamespaceRouteGroup("")]
    [Tags(DeclarativeGroupMetadata.EmptyTag)]
    [DeclarativeStage("empty")]
    public sealed partial class EmptyDeclarativeGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            context.Services.GetRequiredService<DeclarativeMappingProbe>().EmptyConditions++;
            return true;
        }
    }
}

namespace RonSijm.MediumAPI.NamespaceMetadataExample.DeclarativeRoutes.Empty.Items
{
    [NamespaceRouteGroup("/items")]
    [Tags(DeclarativeGroupMetadata.ItemTag)]
    [DeclarativeStage("items")]
    [ResponseHeader("X-Declarative", "items")]
    public sealed partial class DeclarativeItemsGroupDefinition : INamespaceEndpointGroup;

    public sealed class GetDeclarativeItemEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/{itemId:int}";

        public Ok<DeclarativeReply> HandleAsync([FromRoute(Name = "tenantId")] long accountId, [FromRoute] int itemId, DeclarativeMappingProbe probe)
        {
            probe.Calls++;
            return TypedResults.Ok(new DeclarativeReply(accountId, itemId, "item"));
        }
    }
}

namespace RonSijm.MediumAPI.NamespaceMetadataExample.DeclarativeRoutes.Disabled
{
    public readonly struct UnreachableDeclarativeConvention : IMediumApiEndpointConvention
    {
        public static void Configure(Microsoft.AspNetCore.Builder.IEndpointConventionBuilder builder)
        {
            throw new InvalidOperationException("Disabled declarative namespaces must not configure.");
        }
    }

    [NamespaceRouteGroup("/disabled")]
    [EndpointConvention<UnreachableDeclarativeConvention>]
    public sealed partial class DisabledDeclarativeGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            context.Services.GetRequiredService<DeclarativeMappingProbe>().DisabledConditions++;
            return false;
        }
    }
}