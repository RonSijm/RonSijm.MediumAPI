using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.SharedResponsesExample.ResponseSets
{
    [EndpointResponses(499)]
    public abstract class ResponseSetsGroupBase { }

    [EndpointResponses(500, 503)]
    [EndpointResponseFallback(401, typeof(EarlierError))]
    [EndpointResponseFallback(404, typeof(EarlierError))]
    public sealed class ResponseSetsGroupDefinition : ResponseSetsGroupBase, INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return context.Configuration.GetValue("Sample:ResponseSets", false);
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/response-sets").WithTags(SharedResponseMetadata.Tag);
        }
    }

    [EndpointResponse(409)]
    [MarkedResponses(410, 411)]
    [EndpointResponse(412)]
    [EndpointFilter<ResponseSetFilter>]
    [EndpointFilterFactory<ResponseSetFactory>]
    public sealed class ResponseSetValueEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value/{value:int}";

        [ResponseSetPhase("method-before")]
        [EndpointResponse(418)]
        [AccessResponses]
        [MarkedResponses(401)]
        [ResponseSetPhase("method-after")]
        [EndpointResponse(422)]
        [EndpointResponseFallback(403, typeof(MethodError))]
        public Ok<SharedReply> HandleAsync([FromRoute] int value, SharedResponseProbe probe)
        {
            return SharedReplies.Ok(probe, "response-set", value);
        }
    }

    [EndpointResponses(497)]
    public abstract class ResponseSetEndpointBase : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public abstract string Pattern { get; }

        [EndpointResponses(406, 407)]
        public Ok<SharedReply> HandleAsync([FromRoute] int value, SharedResponseProbe probe)
        {
            return SharedReplies.Ok(probe, "response-set-inherited", value);
        }
    }

    public sealed class InheritedResponseSetEndpoint : ResponseSetEndpointBase
    {
        [StringSyntax("Route")]
        public override string Pattern => "/inherited/{value:int}";
    }

    public sealed class TypedResponseSetEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/typed";

        [EndpointResponses(401, 403)]
        [ProducesResponseType<FilterError>(403, "application/json")]
        public Results<Ok<SharedReply>, UnauthorizedHttpResult> HandleAsync(SharedResponseProbe probe)
        {
            return SharedReplies.Ok(probe, "response-set-typed", 0);
        }

        public static void Configure(RouteHandlerBuilder endpoint)
        {
            endpoint.WithMetadata(new EndpointResponseAttribute(403, typeof(ConfiguredError)));
            endpoint.Finally(builder => builder.Metadata.Add(new EndpointResponseAttribute(403, typeof(MethodError))));
        }
    }

    [EndpointResponses]
    public sealed class EmptyResponseSetEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/empty";

        [EndpointResponses]
        public Ok<SharedReply> HandleAsync(SharedResponseProbe probe)
        {
            return SharedReplies.Ok(probe, "response-set-empty", 0);
        }
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.ResponseSets.Disabled
{
    public sealed class UnreachableResponsesAttribute : EndpointResponsesAttribute
    {
        public UnreachableResponsesAttribute() : base(500)
        {
            throw new InvalidOperationException("Disabled response sets must not be constructed.");
        }
    }

    [UnreachableResponses]
    public sealed class DisabledResponseSetsGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return false;
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            throw new InvalidOperationException("Disabled response-set namespaces must not map.");
        }
    }
}