using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResponseFallbackExample.Endpoints.Api.Grouped
{
    [EndpointResponseFallback(StatusCodes.Status404NotFound, typeof(NamespaceFailure))]
    public sealed class GroupedDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/grouped");
        }
    }

    public sealed class GetGroupedPlainEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/plain";

        public Ok<FallbackReply> HandleAsync(FallbackProbe probe)
        {
            return FallbackReplies.Ok("grouped-plain", probe);
        }
    }

    public sealed class GetGroupedTypedEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/typed";

        public Results<Ok<FallbackReply>, NotFound> HandleAsync([FromQuery] bool? absent, FallbackProbe probe)
        {
            return FallbackReplies.Typed("grouped-typed", absent, probe);
        }
    }
}

namespace RonSijm.MediumAPI.ResponseFallbackExample.Endpoints.Api.GroupFiltered
{
    public sealed class GroupFilteredDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/group-filter").AddMediumApiFilter<MissingItemFilter>();
        }
    }

    public sealed class GetGroupFilteredPlainEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/plain";

        public Ok<FallbackReply> HandleAsync([FromQuery] bool? missing, FallbackProbe probe)
        {
            return FallbackReplies.Ok("group-filter-plain", probe);
        }
    }

    public sealed class GetGroupFilteredTypedEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/typed";

        public Results<Ok<FallbackReply>, NotFound> HandleAsync([FromQuery] bool? missing, [FromQuery] bool? absent, FallbackProbe probe)
        {
            return FallbackReplies.Typed("group-filter-typed", absent, probe);
        }
    }
}

namespace RonSijm.MediumAPI.ResponseFallbackExample.Endpoints.Api.Final
{
    [EndpointResponseFallback(StatusCodes.Status404NotFound, typeof(ParentFailure))]
    public sealed class ParentFinalDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/final");
        }
    }
}

namespace RonSijm.MediumAPI.ResponseFallbackExample.Endpoints.Api.Final.Child
{
    [EndpointResponseFallback(StatusCodes.Status404NotFound, typeof(ChildFailure))]
    public sealed class ChildFinalDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/child");
        }
    }

    public sealed class GetChildPlainEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/plain";

        public Ok<FallbackReply> HandleAsync(FallbackProbe probe)
        {
            return FallbackReplies.Ok("child-plain", probe);
        }
    }

    public sealed class GetChildExplicitEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/explicit";

        public Ok<FallbackReply> HandleAsync(FallbackProbe probe)
        {
            return FallbackReplies.Ok("child-explicit", probe);
        }

        public static void Configure(RouteHandlerBuilder endpoint)
        {
            ((IEndpointConventionBuilder)endpoint).Finally(builder => builder.Metadata.Add(new ProducesResponseTypeMetadata(404, typeof(FinalFailure), ["application/x-final-error"])));
        }
    }
}

namespace RonSijm.MediumAPI.ResponseFallbackExample.Endpoints.Api.Disabled
{
    [EndpointResponseFallback(StatusCodes.Status404NotFound)]
    public sealed class DisabledDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return false;
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            throw new InvalidOperationException("Disabled groups must not map.");
        }
    }

    public sealed class GetDisabledEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/disabled";

        public string HandleAsync()
        {
            throw new InvalidOperationException("Disabled handlers must not run.");
        }
    }
}