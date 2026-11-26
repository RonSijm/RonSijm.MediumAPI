using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.EndpointDocumentationExample.SharedQueryRoutes;
using Xunit;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest;

internal static class SharedQueryHost
{
    internal static WebApplication Create(string reference = "generated", bool throwOnBadRequest = false, bool map = true, bool enabled = true, bool alias = false)
    {
        var app = Program.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Configuration["Sample:SharedQueries"] = enabled.ToString();
            builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = throwOnBadRequest);
        }, mapEndpoints: false);
        if (map && reference == "generated") { SharedQuerySample.Map(app); }
        else if (map && enabled)
        {
            var group = Scope(app.MapGroup("/query-sample"));
            var before = reference == "before";
            AddNativeFilter(MapOriginal(group, "/audit-logs", before ? (Delegate)BeforeSearch : NativeSearch)).WithName("SearchSharedAudit");
            AddNativeFilter(MapOriginal(group, "/workspace", before ? (Delegate)BeforeWorkspace : NativeWorkspace)).WithName("GetSharedAuditWorkspace");
        }

        if (map && enabled && alias)
        {
            var group = Scope(app.MapGroup("/query-sample"));
            if (reference == "generated") { group.MapMediumApiEndpoint<SearchSharedAuditEndpoint>("/alias").WithName("SharedAuditAlias"); }
            else { AddNativeFilter(MapOriginal(group, "/alias", reference == "before" ? (Delegate)BeforeSearch : NativeSearch)).WithName("SharedAuditAlias"); }
        }

        return app;
    }

    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    }

    internal static RouteEndpoint Route(WebApplication app, string pattern)
    {
        return Assert.Single(Routes(app), route => route.RoutePattern.RawText == pattern);
    }

    internal static SharedQueryProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<SharedQueryProbe>();
    }

    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    }

    internal static string OpenApi(WebApplication app, string document = DocumentationGroups.Public)
    {
        return Program.GetOpenApiJson(app.Services, document);
    }

    private static RouteGroupBuilder Scope(RouteGroupBuilder group)
    {
        return group.WithGroupName(DocumentationGroups.Public).WithTags("Shared audit queries");
    }

    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapMethods(pattern, new[] { HttpMethods.Get }, handler);
    }

    private static RouteHandlerBuilder AddNativeFilter(RouteHandlerBuilder endpoint)
    {
        endpoint.WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest, typeof(void), Array.Empty<string>()));
        return endpoint.AddEndpointFilter(async (invocation, next) =>
        {
            var probe = invocation.HttpContext.RequestServices.GetRequiredService<SharedQueryProbe>();
            probe.RecordFilter(invocation.HttpContext.RequestAborted, invocation.Arguments.ToArray());
            try { return await next(invocation); }
            catch (SharedQueryValidationException)
            {
                probe.RecordTranslation();
                return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Check the synthetic query.", extensions: new Dictionary<string, object?> { ["code"] = "synthetic-query" });
            }
        });
    }

    [EndpointSummary("Search using shared optional audit queries")]
    private static async Task<Ok<SharedQueryReply>> BeforeSearch([FromQuery] string? search, [FromQuery] long? customerId, [FromQuery] bool? platform, [FromQuery] string? action, [FromQuery] string? entityType, [FromQuery] long? userId, [FromQuery] string? from, [FromQuery] string? to, [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] long? projectId, [FromQuery] string? kind, [FromServices] SharedQueryService auditLogs, CancellationToken cancellationToken)
    {
        auditLogs.Probe.RecordHandler(cancellationToken);
        var request = SharedQueryNormalization.Create(search, customerId, platform, action, entityType, userId, from, to, page, pageSize, projectId, kind);
        return TypedResults.Ok(await auditLogs.SearchAsync(request, cancellationToken));
    }

    [EndpointSummary("Get a workspace using the same optional audit queries")]
    private static async Task<Ok<SharedQueryReply>> BeforeWorkspace([FromQuery] string? search, [FromQuery] long? customerId, [FromQuery] bool? platform, [FromQuery] string? action, [FromQuery] string? entityType, [FromQuery] long? userId, [FromQuery] string? from, [FromQuery] string? to, [FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] long? projectId, [FromQuery] string? kind, [FromServices] SharedQueryService auditLogs, CancellationToken cancellationToken)
    {
        auditLogs.Probe.RecordHandler(cancellationToken);
        var request = SharedQueryNormalization.Create(search, customerId, platform, action, entityType, userId, from, to, page, pageSize, projectId, kind);
        return TypedResults.Ok(await auditLogs.LoadWorkspaceAsync(request, cancellationToken));
    }

    [EndpointSummary("Search using shared optional audit queries")]
    private static async Task<Ok<SharedQueryReply>> NativeSearch([AsParameters] SharedAuditQuery query, [FromServices] SharedQueryService auditLogs, CancellationToken cancellationToken)
    {
        auditLogs.Probe.RecordHandler(cancellationToken);
        var request = SharedQueryNormalization.Create(query.search, query.customerId, query.platform, query.action, query.entityType, query.userId, query.from, query.to, query.page, query.pageSize, query.projectId, query.kind);
        return TypedResults.Ok(await auditLogs.SearchAsync(request, cancellationToken));
    }

    [EndpointSummary("Get a workspace using the same optional audit queries")]
    private static async Task<Ok<SharedQueryReply>> NativeWorkspace([AsParameters] SharedAuditQuery query, [FromServices] SharedQueryService auditLogs, CancellationToken cancellationToken)
    {
        auditLogs.Probe.RecordHandler(cancellationToken);
        var request = SharedQueryNormalization.Create(query.search, query.customerId, query.platform, query.action, query.entityType, query.userId, query.from, query.to, query.page, query.pageSize, query.projectId, query.kind);
        return TypedResults.Ok(await auditLogs.LoadWorkspaceAsync(request, cancellationToken));
    }
}