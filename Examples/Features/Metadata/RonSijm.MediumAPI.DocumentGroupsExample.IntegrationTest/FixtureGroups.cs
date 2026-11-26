using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest
{
    public sealed record FixtureResponse(string Kind);

    public sealed record StageMarker(string Stage);

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UnrelatedGroupNameAttribute : Attribute, IEndpointGroupNameMetadata
    {
        public UnrelatedGroupNameAttribute() => throw new InvalidOperationException("Unrelated group-name attributes must not be constructed.");
        public string EndpointGroupName => "unrelated";
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisabledConstructorAttribute : Attribute, IAuthorizeData
    {
        public DisabledConstructorAttribute() => throw new InvalidOperationException("A disabled namespace must not construct metadata.");
        public string? Policy { get; set; }
        public string? Roles { get; set; }
        public string? AuthenticationSchemes { get; set; }
    }
}

namespace RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest.Fixtures.Ordered
{
    [EndpointGroupName("class")]
    public sealed class OrderedGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/ordered").WithGroupName("map").WithMetadata(new StageMarker("Map"));
    }

    public sealed class GetOrderedValueEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<FixtureResponse> HandleAsync() => TypedResults.Ok(new FixtureResponse("value"));
    }

    public sealed class GetOrderedMethodEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/method";

        [EndpointGroupName("method")]
        public Ok<FixtureResponse> HandleAsync() => TypedResults.Ok(new FixtureResponse("method"));
    }

    public sealed class GetOrderedConfiguredEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/configured";

        [EndpointGroupName("method")]
        public Ok<FixtureResponse> HandleAsync() => TypedResults.Ok(new FixtureResponse("configured"));

        public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithGroupName("configured").WithMetadata(new StageMarker("Configure"));
    }
}

namespace RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest.Fixtures.Ignored
{
    [EndpointGroupName("base")]
    public abstract class GroupNameBase { }

    [UnrelatedGroupName]
    public sealed class IgnoredGroupDefinition : GroupNameBase, INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/ignored");
    }

    [EndpointGroupName("endpoint-class")]
    public sealed class GetIgnoredValueEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/value";

        public Ok<FixtureResponse> HandleAsync() => TypedResults.Ok(new FixtureResponse("ignored"));
    }
}

namespace RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest.Fixtures.Disabled
{
    [EndpointGroupName("disabled")]
    [DisabledConstructor]
    public sealed class DisabledGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => false;
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("Disabled Map must not run.");
    }

    public sealed class GetDisabledValueEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/disabled-parent";

        public Ok<FixtureResponse> HandleAsync() => TypedResults.Ok(new FixtureResponse("disabled"));
    }
}

namespace RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest.Fixtures.Disabled.Child
{
    [EndpointGroupName("disabled-child")]
    [DisabledConstructor]
    public sealed class DisabledChildGroupDefinition : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context) => throw new InvalidOperationException("A disabled parent suppresses descendant conditions.");
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => throw new InvalidOperationException("A disabled descendant must not map.");
    }
}