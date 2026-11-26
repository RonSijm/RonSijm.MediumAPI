using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResponseHeadersExample.SetPolicies
{
    [EndpointFilter<HeaderSetGuard>]
    [SamplePairHeaders("X-Group", "group")]
    [UnrelatedHeaderExample]
    public sealed class HeaderSetsGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return context.Configuration.GetValue("Sample:HeaderSets", false);
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/header-sets").WithTags("Header sets").AddEndpointFilter<HeaderSetObserver>();
        }
    }

    [SamplePairHeaders("X-Class", "class")]
    [UnrelatedHeaderExample]
    public sealed class HeaderSetEntryEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/entry/{kind}";

        [SampleNoCacheHeaders]
        public Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> HandleAsync([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
        {
            return HeaderSetBodies.Create(kind, probe);
        }
    }

    public sealed class HeaderSetSelectedEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/selected/{kind}";

        [SampleSelectedCacheHeaders]
        public Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> HandleAsync([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
        {
            return HeaderSetBodies.Create(kind, probe);
        }
    }

    [EndpointFilter<GroupedHeaderFilter>]
    public sealed class HeaderSetFilterEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/filter/{kind}";

        [SampleHeader("X-Handler", "entry")]
        public Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> HandleAsync([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
        {
            return HeaderSetBodies.Create(kind, probe);
        }
    }

    [EndpointFilterFactory<GroupedHeaderFactory>]
    public sealed class HeaderSetFactoryEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/factory/{kind}";

        public Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> HandleAsync([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
        {
            return HeaderSetBodies.Create(kind, probe);
        }
    }

    [SampleHeader("X-Ignored-Base-Class", "ignored")]
    public abstract class HeaderSetMethodBase : IGetEndpoint
    {
        [StringSyntax("Route")]
        public abstract string Pattern { get; }

        [SampleNoCacheHeaders]
        [SampleHeader("X-Inherited", "base")]
        public virtual Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> HandleAsync([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
        {
            return HeaderSetBodies.Create(kind, probe);
        }
    }

    public sealed class InheritedHeaderSetEndpoint : HeaderSetMethodBase
    {
        [StringSyntax("Route")]
        public override string Pattern => "/inherited/{kind}";
    }

    public sealed class OverrideHeaderSetEndpoint : HeaderSetMethodBase
    {
        [StringSyntax("Route")]
        public override string Pattern => "/override/{kind}";

        [SampleHeader("X-Inherited", "override")]
        [SampleNoCacheHeaders]
        public override Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound> HandleAsync([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, HeaderSetProbe probe)
        {
            return HeaderSetBodies.Create(kind, probe);
        }
    }
}

namespace RonSijm.MediumAPI.ResponseHeadersExample.SetPolicies.Disabled
{
    public sealed class UnreachableHeaderSetAttribute : ResponseHeadersAttribute
    {
        public UnreachableHeaderSetAttribute() : base(Array.Empty<ResponseHeaderAttribute>())
        {
            throw new InvalidOperationException("A disabled namespace must not construct header sets.");
        }
    }

    [UnreachableHeaderSet]
    public sealed class DisabledHeaderSetsGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return false;
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            throw new InvalidOperationException("A disabled namespace must not map.");
        }
    }
}