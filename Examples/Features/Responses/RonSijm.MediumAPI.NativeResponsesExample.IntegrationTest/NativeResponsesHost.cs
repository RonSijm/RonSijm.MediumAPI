using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.NativeResponsesExample.Api;
using RonSijm.MediumAPI.NativeResponsesExample.Manual;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.NativeResponsesExample.Program;

namespace RonSijm.MediumAPI.NativeResponsesExample.IntegrationTest;

internal static class NativeResponsesHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        }, mapEndpoints: mapEndpoints && !native);
        if (native && mapEndpoints)
        {
            MapNative(app);
        }

        return app;
    }

    internal static RouteEndpoint[] Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().Where(endpoint => endpoint.RoutePattern.RawText != "/descriptions").ToArray();
    }

    internal static RouteEndpoint Route(WebApplication app, string path)
    {
        return Assert.Single(Routes(app), route => route.RoutePattern.RawText == path);
    }

    internal static ResponseProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<ResponseProbe>();
    }

    internal static string[] MvcValues(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>().Select(metadata =>
        {
            var contentTypes = new MediaTypeCollection();
            metadata.SetContentTypes(contentTypes);
            return string.Join("|", metadata.GetType().FullName, metadata.StatusCode, metadata.Type?.FullName, string.Join(",", contentTypes));
        }).ToArray();
    }

    internal static ApiDescription Description(WebApplication app, string path)
    {
        return Assert.Single(app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items), description => "/" + description.RelativePath == path);
    }

    internal static string[] DescriptionValues(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items)
            .Where(description => description.RelativePath != "descriptions")
            .Select(description => description.HttpMethod + "|" + description.RelativePath + "|" + string.Join(";", description.SupportedResponseTypes.OrderBy(response => response.StatusCode).Select(response => response.StatusCode + ":" + response.Type?.FullName + ":" + string.Join(",", response.ApiResponseFormats.Select(format => format.MediaType)))))
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private static void MapNative(WebApplication app)
    {
        var group = app.MapGroup("/responses").WithMetadata(new ProducesResponseTypeAttribute(typeof(GroupNotice), 503), new ProducesResponseTypeAttribute<GroupNotice>(429, "application/json", "application/vnd.catalog-notice+json"));
        MapOriginal(group, "/native/{id:int}", new GetMethodResponseEndpoint().HandleAsync);
        MapOriginal(group, "/catalog/{id:int}", new GetCatalogResponseEndpoint().HandleAsync).WithName("GetCatalogResponse")
            .WithMetadata(new ProducesResponseTypeAttribute(typeof(DeclaredFailure), 400, "application/problem+json"), new ProducesResponseTypeAttribute<DeclaredFailure>(404), new ProducesResponseTypeAttribute<DeclaredFailure>(418, "application/json", "application/vnd.catalog-error+json", "application/json"), new ProducesResponseTypeAttribute(typeof(GroupNotice), 418, "text/plain"));
        MapOriginal(group, "/defaults", new GetDefaultResponseEndpoint().HandleAsync).WithName("GetDefaultResponse")
            .WithMetadata(new ProducesResponseTypeAttribute(202), new ProducesResponseTypeAttribute<DeclaredFailure>(404), new ProducesResponseTypeAttribute(409) { Type = typeof(DeclaredFailure), StatusCode = 422 });
        MapOriginal(group, "/method/{id:int}", new GetMethodResponseEndpoint().HandleAsync).WithName("GetMethodResponse");
        Missing(MapOriginal(group, "/filtered/{id:int}", new GetFilteredResponseEndpoint().HandleAsync).WithName("GetFilteredResponse"));
        // The independent pre-extraction wrapper retains the same declared native result union.
        group.MapGet("/migrated/{id:int}", LegacyMigrated).WithName("GetMigratedResponse")
            .WithMetadata(new ProducesResponseTypeAttribute<DeclaredFailure>(404, "application/vnd.catalog-error+json"));
        var mixed = MapOriginal(group, "/mixed/{id:int}", new GetMixedResponseEndpoint().HandleAsync).WithName("GetMixedResponse")
            .WithMetadata(new ProducesResponseTypeAttribute<DeclaredFailure>(200, "application/vnd.declared-success+json"), new ProducesResponseTypeAttribute<DeclaredFailure>(404, "application/vnd.declared-error+json"));
        mixed.Add(builder =>
        {
            if (!builder.Metadata.OfType<IProducesResponseTypeMetadata>().Any(metadata => metadata.StatusCode == 404))
            {
                builder.Metadata.Add(new ProducesResponseTypeMetadata(404, typeof(FallbackNotice), ["application/json"]));
            }
        });
        mixed.AddEndpointFilter<MissingResponseFallbackFilter>();
        MapOriginal(group, "/configured/{id:int}", new GetConfiguredResponseEndpoint().HandleAsync).WithName("GetConfiguredResponse")
            .WithMetadata(new ProducesResponseTypeAttribute<ConfiguredNotice>(425, "application/vnd.configured+json"), new ProducesResponseTypeAttribute<DeclaredFailure>(425, "application/vnd.declared+json"))
            .AddEndpointFilter((context, next) =>
            {
                context.HttpContext.Response.Headers["X-Native-Configured"] = "present";
                return next(context);
            }).AddEndpointFilter<ConfiguredResponseFilter>();
        MapOriginal(group, "/custom/{id:int}", new GetCustomResponseEndpoint().HandleAsync).WithName("GetCustomResponse")
            .WithMetadata(new CatalogResponseAttribute(451) { Label = "catalog-contract" });
        foreach (var path in new[] { "/aliases/first/{id:int}", "/aliases/second/{id:int}" })
        {
            var aliasGroup = app.MapGroup("").WithMetadata(new ProducesResponseTypeAttribute<DeclaredFailure>(410, "application/json"));
            Missing(MapOriginal(aliasGroup, path, new AliasResponseEndpoint().HandleAsync));
        }

        Missing(MapOriginal(app, "/fluent/{id:int}", ExampleProgram.Fluent));
    }

    // Passing the original Delegate avoids the upstream cross-assembly method-group analyzer bug.
    // The native mapper still receives its original MethodInfo, target and parameter attributes.
    private static RouteHandlerBuilder MapOriginal(IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern, Delegate handler)
    {
        return endpoints.MapGet(pattern, handler);
    }

    private static void Missing(RouteHandlerBuilder endpoint)
    {
        endpoint.WithMetadata(new ProducesResponseTypeAttribute<DeclaredFailure>(404, "application/vnd.catalog-error+json")).AddEndpointFilter<MissingItemFilter>();
    }

    [ProducesResponseType(401)]
    private static Results<Ok<CatalogItem>, NotFound<MissingItem>> LegacyMigrated(int id, ResponseProbe probe)
    {
        probe.VisitFilter();
        if (id <= 0)
        {
            return TypedResults.NotFound(new MissingItem("missing"));
        }

        return probe.Reply(id, "migrated");
    }
}