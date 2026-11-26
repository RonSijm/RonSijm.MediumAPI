using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI.NativeResponsesExample.IntegrationTest.Fixtures.Ignored
{
    [UnrelatedThrowing]
    [DerivedThrowingResponse]
    [DerivedThrowingGenericResponse]
    public sealed class IgnoredResponseGroup : InheritedGroupBase, INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/ignored-group");
        }
    }
}

namespace RonSijm.MediumAPI.NativeResponsesExample.IntegrationTest.Fixtures.Custom
{
    [CatalogResponse(452, Label = "group-field")]
    [ProducesResponseType<DeclaredFailure>(453)]
    public sealed class CustomResponseGroup : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            return endpoints.MapGroup("/custom-group");
        }
    }
}

namespace RonSijm.MediumAPI.NativeResponsesExample.IntegrationTest.Fixtures.Disabled
{
    [ProducesResponseType<DeclaredFailure>(400, "not a media type")]
    public sealed class DisabledResponseGroup : INamespaceEndpointGroup
    {
        public static bool ShouldMap(EndpointMappingContext context)
        {
            return false;
        }

        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
        {
            throw new InvalidOperationException("A disabled definition must never be mapped.");
        }
    }
}