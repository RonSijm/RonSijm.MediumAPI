using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.SharedQueryRoutes;

[NamespaceRouteGroup("/query-sample")]
[Tags("Shared audit queries")]
[EndpointGroupName(DocumentationGroups.Public)]
public sealed partial class SharedQueryGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        return context.Configuration.GetValue("Sample:SharedQueries", false);
    }
}

[EndpointPattern("/audit-logs")]
public sealed partial class SearchSharedAuditEndpoint : IGetEndpoint
{
    [EndpointSummary("Search using shared optional audit queries")]
    [EndpointResponse(StatusCodes.Status400BadRequest)]
    [EndpointFilter<SharedQueryValidationFilter>]
    public async Task<Ok<SharedQueryReply>> HandleAsync([AsParameters] SharedAuditQuery query, [FromServices] SharedQueryService auditLogs, CancellationToken cancellationToken)
    {
        auditLogs.Probe.RecordHandler(cancellationToken);
        var request = SharedQueryNormalization.Create(query.search, query.customerId, query.platform, query.action, query.entityType, query.userId, query.from, query.to, query.page, query.pageSize, query.projectId, query.kind);
        return TypedResults.Ok(await auditLogs.SearchAsync(request, cancellationToken));
    }
}

[EndpointPattern("/workspace")]
public sealed partial class GetSharedAuditWorkspaceEndpoint : IGetEndpoint
{
    [EndpointSummary("Get a workspace using the same optional audit queries")]
    [EndpointResponse(StatusCodes.Status400BadRequest)]
    [EndpointFilter<SharedQueryValidationFilter>]
    public async Task<Ok<SharedQueryReply>> HandleAsync([AsParameters] SharedAuditQuery query, [FromServices] SharedQueryService auditLogs, CancellationToken cancellationToken)
    {
        auditLogs.Probe.RecordHandler(cancellationToken);
        var request = SharedQueryNormalization.Create(query.search, query.customerId, query.platform, query.action, query.entityType, query.userId, query.from, query.to, query.page, query.pageSize, query.projectId, query.kind);
        return TypedResults.Ok(await auditLogs.LoadWorkspaceAsync(request, cancellationToken));
    }
}