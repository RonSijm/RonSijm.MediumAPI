using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest.ExplicitPatternRoutes
{
    [NamespaceRouteGroup("/patterns")]
    public sealed partial class ExplicitPatternGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return false;
        }
    }

    public sealed class ExplicitStatusEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/status";

        [EndpointName("GetPatternStatus")]
        [EndpointSummary("Read the pattern sample status")]
        [EndpointFilter<EndpointPatternFilter>]
        [ResponseHeader("CacheControl", "no-store")]
        public Ok<EndpointPatternReply> HandleAsync([FromServices] EndpointPatternProbe probe, HttpContext context)
        {
            probe.RecordHandler("status");
            return TypedResults.Ok(new EndpointPatternReply(null, null, "status"));
        }
    }
}

namespace RonSijm.MediumAPI.NamespaceMetadataExample.IntegrationTest.ExplicitPatternRoutes.Customers
{
    [NamespaceRouteGroup("/customers/{customerId:long}")]
    public sealed partial class ExplicitCustomerGroupDefinition : INamespaceEndpointGroup { }

    public sealed class ExplicitItemEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => EndpointPatternMetadata.Item;

        [EndpointName("GetPatternItem")]
        [EndpointSummary("Read a constrained item from a declared endpoint pattern")]
        [EndpointFilter<EndpointPatternFilter>]
        [ResponseHeader("CacheControl", "no-store")]
        public Ok<EndpointPatternReply> HandleAsync([FromRoute] long customerId, [FromRoute] long itemId, HttpContext context, [FromServices] EndpointPatternProbe probe, CancellationToken token)
        {
            probe.RecordHandler("item");
            return TypedResults.Ok(new EndpointPatternReply(customerId, itemId, "item"));
        }
    }

    public sealed class ExplicitLookupEndpoint : IPostEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => EndpointPatternMetadata.Lookup;

        [EndpointName("PostPatternLookup")]
        [EndpointSummary("Look up a constrained item with an explicit missing outcome")]
        [EndpointFilter<EndpointPatternFilter>]
        [ResponseHeader("CacheControl", "no-store")]
        public Results<Ok<EndpointPatternReply>, NotFound> HandleAsync([FromRoute] long customerId, [FromRoute] long itemId, HttpContext context, [FromServices] EndpointPatternProbe probe, CancellationToken token)
        {
            probe.RecordHandler("lookup");
            if (itemId == 0) { return TypedResults.NotFound(); }

            return TypedResults.Ok(new EndpointPatternReply(customerId, itemId, "lookup"));
        }
    }
}