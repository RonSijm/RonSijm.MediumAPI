using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.OpenApiMigrationExample.Program;

namespace RonSijm.MediumAPI.OpenApiMigrationExample.IntegrationTest;

internal static class MigrationHost
{
    internal const string ProductPath = "/api/products/{productId}";
    internal const string AliasPath = "/compat/products/{productId}";
    internal const string ProductPointer = "/paths/~1api~1products~1{productId}/get";

    internal static WebApplication Create(MappingStyle style) => ExampleProgram.Build(style, configure: builder =>
    {
        builder.WebHost.UseTestServer();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
    });

    internal static JsonObject OpenApi(WebApplication app) => Assert.IsType<JsonObject>(JsonNode.Parse(ExampleProgram.GetOpenApiJson(app.Services)));
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static CatalogService Catalog(WebApplication app) => app.Services.GetRequiredService<CatalogService>();

    internal static JsonObject Object(JsonNode? node) => Assert.IsType<JsonObject>(node);
    internal static JsonArray Array(JsonNode? node) => Assert.IsType<JsonArray>(node);
    internal static JsonObject GetOperation(JsonObject document) => Object(Object(Object(document["paths"])[ProductPath])["get"]);
    internal static JsonObject PostOperation(JsonObject document) => Object(Object(Object(document["paths"])["/api/products"])["post"]);

    internal static JsonObject Parameter(JsonObject operation, string name) => Object(Assert.Single(Array(operation["parameters"]), parameter => parameter?["name"]?.GetValue<string>() == name));

    internal static int ParameterIndex(JsonObject operation, string name)
    {
        var parameters = Array(operation["parameters"]);
        return Enumerable.Range(0, parameters.Count).Single(index => parameters[index]?["name"]?.GetValue<string>() == name);
    }

    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
}