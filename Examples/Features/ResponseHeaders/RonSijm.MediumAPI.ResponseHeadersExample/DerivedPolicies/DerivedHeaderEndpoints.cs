using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ResponseHeadersExample.DerivedPolicies
{
    [SampleHeader("X-Ignored-Group-Base", "ignored")]
    public abstract class DerivedHeaderGroupBase;

    [EndpointFilter<DerivedHeaderGuard>]
    [SampleHeader("X-Derived-Group", "group")]
    [SampleHeader("X-Derived-Group-Selected", "selected", ResultTypes = new[] { typeof(Ok<>), typeof(Created<>) })]
    public sealed class DerivedHeaderGroupDefinition : DerivedHeaderGroupBase, INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return context.Services.GetRequiredService<IConfiguration>().GetValue("Sample:DerivedHeaders", false);
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/derived").WithTags("Derived headers").AddEndpointFilter<DerivedHeaderObserver>();
        }
    }

    [SampleHeader("X-Class", "constant")]
    [SampleNoStore(ResultTypes = new[] { typeof(Ok<>), typeof(Created<>) })]
    public sealed class DerivedEntryEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/items/{kind}";

        [SampleNoCachePragma]
        [SampleHeader("X-Values", "one", "two", ResultType = typeof(Ok<>))]
        [SampleHeader("X-Method", "selected", ResultType = typeof(Created<>))]
        public Results<Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>>, NotFound> HandleAsync([FromRoute] string kind, [FromQuery] int? limit, DerivedHeaderProbe probe)
        {
            return DerivedHeaderBodies.Create(kind, probe);
        }
    }

    [EndpointFilter<DerivedHeaderFilter>]
    public sealed class DerivedFilterEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/filtered/{kind}";

        [SampleHeader("X-Handler-Only", "handler")]
        public Results<Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>>, NotFound> HandleAsync([FromRoute] string kind, DerivedHeaderProbe probe)
        {
            return DerivedHeaderBodies.Create(kind, probe);
        }
    }

    [EndpointFilterFactory<DerivedHeaderFactory>]
    public sealed class DerivedFactoryEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/factory/{kind}";

        public Results<Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>>, NotFound> HandleAsync([FromRoute] string kind, DerivedHeaderProbe probe)
        {
            return DerivedHeaderBodies.Create(kind, probe);
        }
    }

    [SampleHeader("X-Ignored-Endpoint-Base", "ignored")]
    public abstract class DerivedEndpointBase : IGetEndpoint
    {
        [StringSyntax("Route")]
        public abstract string Pattern { get; }

        [SampleHeader("X-Inherited-Method", "inherited")]
        [SampleNoCachePragma]
        public Results<Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>>, NotFound> HandleAsync([FromRoute] string kind, DerivedHeaderProbe probe)
        {
            return DerivedHeaderBodies.Create(kind, probe);
        }
    }

    public sealed class InheritedDerivedEndpoint : DerivedEndpointBase
    {
        [StringSyntax("Route")]
        public override string Pattern => "/inherited/{kind}";
    }
}

namespace RonSijm.MediumAPI.ResponseHeadersExample.DerivedPolicies.Disabled
{
    public sealed class UnreachableDerivedHeaderAttribute : ResponseHeaderAttribute
    {
        public UnreachableDerivedHeaderAttribute() : base("X-Unreachable", "never")
        {
            throw new InvalidOperationException("A disabled namespace must not construct its header declaration.");
        }
    }

    [UnreachableDerivedHeader]
    public sealed class DisabledDerivedHeaderGroupDefinition : INamespaceEndpointGroup
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