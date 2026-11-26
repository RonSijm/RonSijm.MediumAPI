using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.EndpointDocumentationExample.Program;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest;

internal static class EndpointDocumentationHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true)
    {
        var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer(), mapEndpoints: mapEndpoints && !native);
        if (native && mapEndpoints)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), route => route.RoutePattern.RawText == path);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static ApiDescription[] ApiDescriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items).ToArray();
    internal static DocumentationProbe Probe(WebApplication app) => app.Services.GetRequiredService<DocumentationProbe>();
    internal static JsonObject Object(JsonNode? node) => Assert.IsType<JsonObject>(node);
    internal static JsonObject OpenApi(WebApplication app, string documentName) => Object(JsonNode.Parse(ExampleProgram.GetOpenApiJson(app.Services, documentName)));
    internal static string[] Paths(JsonObject document) => Object(document["paths"]).Select(pair => pair.Key).OrderBy(path => path, StringComparer.Ordinal).ToArray();
    internal static string[] MetadataInventory(WebApplication app) => Routes(app).Select(route => string.Join("|", route.RoutePattern.RawText, route.Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName ?? "<null>", string.Join(",", route.Metadata.GetMetadata<ITagsMetadata>()?.Tags ?? []), route.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>()?.ExcludeFromDescription ?? false)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    internal static async Task<DocumentationResponse> Read(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DocumentationResponse>())!;
    }

    private static RouteGroupBuilder ClassGroup(IEndpointRouteBuilder parent, string documentName, string[] tags, bool excluded = false)
    {
        var group = parent.MapGroup("");
        group.WithMetadata(new TagsAttribute(tags), new EndpointGroupNameAttribute(documentName));
        if (excluded)
        {
            group.WithMetadata(new ExcludeFromDescriptionAttribute());
        }

        return group;
    }

    private static void MapNativeReference(WebApplication app)
    {
        var catalog = app.MapGroup("/documentation");
        catalog.WithMetadata(new TagsAttribute(DocumentationTags.Group), new EndpointGroupNameAttribute(DocumentationGroups.Public));
        // Native empty groups put class-equivalent metadata before inference and methods.
        ClassGroup(catalog, DocumentationGroups.Public, [DocumentationTags.Catalog, DocumentationTags.Read]).MapGet("/class", NativePublic).WithName("GetClassPublic");
        ClassGroup(catalog, DocumentationGroups.Admin, [DocumentationTags.Admin]).MapGet("/admin", NativeAdmin).WithName("GetClassAdmin");
        ClassGroup(catalog, DocumentationGroups.Public, [DocumentationTags.Catalog, DocumentationTags.Read]).MapGet("/method", NativeMethod).WithName("GetMethodOverride");
        ClassGroup(catalog, DocumentationGroups.Admin, [DocumentationTags.Admin]).MapGet("/configured", NativeConfigured).WithName("GetConfiguredOverride").WithGroupName(DocumentationGroups.Public).WithTags(DocumentationTags.Configured, DocumentationTags.Read);
        catalog.MapGet("/inherited", NativeInherited).WithName("GetInheritedDefaults");
        ClassGroup(catalog, DocumentationGroups.Admin, [DocumentationTags.Hidden], excluded: true).MapGet("/hidden", NativeHidden).WithName("GetHidden");
        ClassGroup(catalog, DocumentationGroups.Unmatched, [DocumentationTags.Unmatched]).MapGet("/unmatched", NativeUnmatched).WithName("GetUnmatched");
        var shared = app.MapGroup("");
        shared.WithMetadata(new TagsAttribute(DocumentationTags.Shared, DocumentationTags.Read));
        shared.MapGet("/shared", NativeShared).WithName("GetShared");
    }

    private static Ok<DocumentationResponse> NativePublic(DocumentationProbe probe) => TypedResults.Ok(probe.Record("public"));
    private static Ok<DocumentationResponse> NativeAdmin(DocumentationProbe probe) => TypedResults.Ok(probe.Record("admin"));
    private static Ok<DocumentationResponse> NativeConfigured(DocumentationProbe probe) => TypedResults.Ok(probe.Record("configured"));
    private static Ok<DocumentationResponse> NativeInherited(DocumentationProbe probe) => TypedResults.Ok(probe.Record("inherited"));
    private static Ok<DocumentationResponse> NativeHidden(DocumentationProbe probe) => TypedResults.Ok(probe.Record("hidden"));
    private static Ok<DocumentationResponse> NativeUnmatched(DocumentationProbe probe) => TypedResults.Ok(probe.Record("unmatched"));
    private static Ok<DocumentationResponse> NativeShared(DocumentationProbe probe) => TypedResults.Ok(probe.Record("shared"));

    [Tags(DocumentationTags.Method)]
    [EndpointGroupName(DocumentationGroups.Admin)]
    private static Ok<DocumentationResponse> NativeMethod(DocumentationProbe probe) => TypedResults.Ok(probe.Record("method"));
}