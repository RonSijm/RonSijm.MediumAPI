using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AsParametersExample.BoundReadRoutes;

[NamespaceRouteGroup("/api")]
public sealed partial class BoundReadGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        return context.Configuration.GetValue("Sample:BoundRouteReads", false);
    }
}

public sealed class GetBoundReadDetailEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => BoundReadMetadata.Pattern;

    [Tags(BoundReadMetadata.Tag)]
    [EndpointFilter<BoundReadExceptionFilter>]
    [EndpointFilter<AggregateArgumentFilter>]
    public async Task<Results<Ok<BoundDetail>, NotFound>> HandleAsync([AsParameters] BoundRunRoute route, [FromServices] BoundDetailService details, CancellationToken token)
    {
        var detail = await details.GetAsync(route.CustomerId, route.RunId, token);
        if (detail.ProjectId != route.ProjectId || detail.VersionId != route.VersionId) { return TypedResults.NotFound(); }

        return TypedResults.Ok(detail);
    }
}

public sealed class GetBoundReadVerificationEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => BoundReadMetadata.Pattern + "/verification";

    [Tags(BoundReadMetadata.Tag)]
    [EndpointFilter<BoundReadExceptionFilter>]
    [EndpointFilter<AggregateArgumentFilter>]
    public async Task<Results<Ok<BoundVerification>, NotFound>> HandleAsync([AsParameters] BoundRunRoute route, [FromServices] BoundDetailService details, [FromServices] BoundVerificationService verification, CancellationToken token)
    {
        var detail = await details.GetAsync(route.CustomerId, route.RunId, token);
        if (detail.ProjectId != route.ProjectId || detail.VersionId != route.VersionId) { return TypedResults.NotFound(); }

        return TypedResults.Ok(await verification.GetAsync(route.CustomerId, route.ProjectId, route.RunId, token));
    }
}