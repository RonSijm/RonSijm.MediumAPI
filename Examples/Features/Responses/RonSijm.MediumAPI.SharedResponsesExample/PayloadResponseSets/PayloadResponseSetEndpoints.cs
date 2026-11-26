using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.SharedResponsesExample.PayloadResponseSets;

[EndpointResponse(501)]
[PayloadProblemResponses(502, 503)]
[EndpointResponseFallback(400, typeof(EarlierError))]
[EndpointResponseFallback(404, typeof(EarlierError))]
public sealed class PayloadResponseSetsGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        return context.Configuration.GetValue("Sample:PayloadResponseSets", false);
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/payload-response-sets").WithTags(SharedResponseMetadata.Tag);
    }
}

[EndpointResponse(408)]
[PayloadProblemResponses(409, 410)]
[EndpointFilter<PayloadResponseFilter>]
[EndpointFilterFactory<PayloadResponseFactory>]
public sealed class PayloadResponseEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value/{value:int}";

    [ResponseSetPhase("payload-before")]
    [PayloadProblemResponses(400, 422)]
    [FormatResponses]
    [ResponseSetPhase("payload-after")]
    [EndpointResponseFallback(422, typeof(MethodError))]
    public Ok<SharedReply> HandleAsync([FromRoute] int value, SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "payload-response-set", value);
    }
}

public sealed class MixedPayloadResponseEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/mixed";

    [PayloadProblemResponses(400, 409)]
    [ProducesResponseType<FilterError>(409, "application/json")]
    public Results<Ok<SharedReply>, BadRequest<FilterError>> HandleAsync(SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "payload-response-set-mixed", 0);
    }

    public static void Configure(RouteHandlerBuilder endpoint)
    {
        endpoint.WithMetadata(new EndpointResponseAttribute(409, typeof(ConfiguredError)));
        endpoint.Finally(builder => builder.Metadata.Add(new FormatResponseAttribute("final", 409, typeof(MethodError), "text/plain", "application/json")));
    }
}

[EmptyPayloadResponses]
public sealed class EmptyPayloadResponseEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/empty";

    [EmptyPayloadResponses]
    public Ok<SharedReply> HandleAsync(SharedResponseProbe probe)
    {
        return SharedReplies.Ok(probe, "payload-response-set-empty", 0);
    }
}