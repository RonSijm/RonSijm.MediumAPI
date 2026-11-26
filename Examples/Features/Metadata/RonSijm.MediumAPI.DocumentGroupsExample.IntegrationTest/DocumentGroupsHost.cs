using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.DocumentGroupsExample.Program;

namespace RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest;

internal static class DocumentGroupsHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true)
    {
        var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer(), mapEndpoints: mapEndpoints && !native);
        if (mapEndpoints && native)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), route => route.RoutePattern.RawText == path);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static DocumentProbe Probe(WebApplication app) => app.Services.GetRequiredService<DocumentProbe>();
    internal static JsonObject Object(JsonNode? node) => Assert.IsType<JsonObject>(node);
    internal static JsonObject OpenApi(WebApplication app, string documentName) => Object(JsonNode.Parse(ExampleProgram.GetOpenApiJson(app.Services, documentName)));
    internal static string[] Paths(JsonObject document) => Object(document["paths"]).Select(pair => pair.Key).OrderBy(path => path, StringComparer.Ordinal).ToArray();
    internal static string[] GroupInventory(WebApplication app) => Routes(app).Select(route => route.RoutePattern.RawText + "=" + (route.Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName ?? "<null>")).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    internal static async Task<DocumentResponse> Read(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DocumentResponse>())!;
    }

    internal static RouteGroupBuilder MapNativeCatalog(IEndpointRouteBuilder endpoints, Action<RouteGroupBuilder>? configure = null)
    {
        var catalog = endpoints.MapGroup("/catalog").WithGroupName(DocumentGroups.Public).WithTags(DocumentTags.Endpoints);
        configure?.Invoke(catalog);
        catalog.MapGet("/value", (DocumentProbe probe) => TypedResults.Ok(probe.Record("catalog"))).WithName("GetCatalogValue");
        catalog.MapGet("/detail", NativeDetail).WithName("GetCatalogDetail");
        var admin = catalog.MapGroup("/admin").WithGroupName(DocumentGroups.Admin);
        admin.MapGet("/value", (DocumentProbe probe) => TypedResults.Ok(probe.Record("admin"))).WithName("GetAdminValue");
        var child = catalog.MapGroup("/child");
        child.MapGet("/value", (DocumentProbe probe) => TypedResults.Ok(probe.Record("child"))).WithName("GetChildValue");
        catalog.MapGet("/hidden", NativeHidden).WithName("GetHiddenCatalogValue");
        return catalog;
    }

    internal static void MapNativeReference(WebApplication app)
    {
        MapNativeCatalog(app);
        app.MapGet("/shared", (DocumentProbe probe) => TypedResults.Ok(probe.Record("shared"))).WithTags(DocumentTags.Endpoints).WithName("GetSharedValue");
        var experimental = app.MapGroup("/experimental").WithGroupName(DocumentGroups.Unmatched).WithTags(DocumentTags.Endpoints);
        experimental.MapGet("/value", (DocumentProbe probe) => TypedResults.Ok(probe.Record("experimental"))).WithName("GetExperimentalValue");
    }

    [EndpointGroupName(DocumentGroups.Admin)]
    private static Ok<DocumentResponse> NativeDetail(DocumentProbe probe) => TypedResults.Ok(probe.Record("detail"));

    [ExcludeFromDescription]
    private static Ok<DocumentResponse> NativeHidden(DocumentProbe probe) => TypedResults.Ok(probe.Record("hidden"));
}