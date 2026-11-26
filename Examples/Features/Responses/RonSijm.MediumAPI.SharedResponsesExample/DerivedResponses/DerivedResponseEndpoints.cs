using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.SharedResponsesExample.DerivedResponses
{
    [ProblemResponse(503)]
    [MarkedProblemResponse(502)]
    [EndpointResponseFallback(404, typeof(EarlierError))]
    public sealed class DerivedResponseGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return context.Services.GetRequiredService<IConfiguration>().GetValue("Sample:DerivedResponses", false);
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/derived-responses").WithTags(SharedResponseMetadata.Tag);
        }
    }

    [ProblemResponse(400)]
    [MarkedProblemResponse(409)]
    [EndpointFilter<DerivedProblemFilter>]
    [EndpointFilterFactory<DerivedProblemFactory>]
    public sealed class DerivedProblemEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value/{value:int}";

        [ProblemResponse(404)]
        [MarkedProblemResponse(422)]
        public Ok<SharedReply> HandleAsync([FromRoute] int value, SharedResponseProbe probe)
        {
            return SharedReplies.Ok(probe, "derived", value);
        }
    }

    [ProblemResponse(451)]
    public abstract class DerivedResponseBase : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public abstract string Pattern { get; }

        [ProblemResponse(412)]
        [MarkedProblemResponse(415)]
        public Ok<SharedReply> HandleAsync([FromRoute] int value, SharedResponseProbe probe)
        {
            return SharedReplies.Ok(probe, "derived-inherited", value);
        }
    }

    public sealed class InheritedDerivedResponseEndpoint : DerivedResponseBase
    {
        [StringSyntax("Route")]
        public override string Pattern => "/inherited/{value:int}";
    }

    [ProblemResponse(409)]
    public sealed class MixedDerivedResponseEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/mixed";

        [ProducesResponseType<FilterError>(409, "application/json")]
        public Ok<SharedReply> HandleAsync(SharedResponseProbe probe)
        {
            return SharedReplies.Ok(probe, "derived-mixed", 0);
        }
    }

    [DerivedResponse(418)]
    [DerivedResponse(419, typeof(FilterError))]
    [DerivedResponse(420, typeof(string), "text/plain", "application/json")]
    public sealed class DerivedFormatsEndpoint : IGetEndpoint, IManuallyMappedEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/formats";

        public Ok<SharedReply> HandleAsync(SharedResponseProbe probe)
        {
            return SharedReplies.Ok(probe, "derived-formats", 0);
        }
    }
}

namespace RonSijm.MediumAPI.SharedResponsesExample.DerivedResponses.Disabled
{
    public sealed class UnreachableDerivedResponseAttribute : EndpointResponseAttribute
    {
        public UnreachableDerivedResponseAttribute() : base(500)
        {
            throw new InvalidOperationException("Disabled response declarations must not be constructed.");
        }
    }

    [UnreachableDerivedResponse]
    public sealed class DisabledDerivedResponseGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return false;
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            throw new InvalidOperationException("Disabled response namespaces must not map.");
        }
    }
}