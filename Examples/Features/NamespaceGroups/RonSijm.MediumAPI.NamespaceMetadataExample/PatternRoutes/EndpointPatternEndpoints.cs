using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.NamespaceMetadataExample.PatternRoutes
{
    [NamespaceRouteGroup("/patterns")]
    [Tags(EndpointPatternMetadata.Tag)]
    [Authorize(Policy = EndpointPatternMetadata.Policy)]
    public sealed partial class EndpointPatternGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return context.Configuration.GetValue(EndpointPatternMetadata.Flag, false);
        }
    }

    [EndpointPattern("/status")]
    public sealed partial class GetPatternStatusEndpoint : IGetEndpoint
    {
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

namespace RonSijm.MediumAPI.NamespaceMetadataExample.PatternRoutes.Customers
{
    [NamespaceRouteGroup("/customers/{customerId:long}")]
    public sealed partial class EndpointPatternCustomerGroupDefinition : INamespaceEndpointGroup { }

    [EndpointPattern(EndpointPatternMetadata.Item)]
    public sealed partial class GetPatternItemEndpoint : IGetEndpoint
    {
        [EndpointSummary("Read a constrained item from a declared endpoint pattern")]
        [EndpointFilter<EndpointPatternFilter>]
        [ResponseHeader("CacheControl", "no-store")]
        public Ok<EndpointPatternReply> HandleAsync([FromRoute] long customerId, [FromRoute] long itemId, HttpContext context, [FromServices] EndpointPatternProbe probe, CancellationToken token)
        {
            probe.RecordHandler("item");
            return TypedResults.Ok(new EndpointPatternReply(customerId, itemId, "item"));
        }
    }

    [EndpointPattern(EndpointPatternMetadata.Lookup)]
    public sealed partial class PostPatternLookupEndpoint : IPostEndpoint
    {
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