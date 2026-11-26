using Xunit;
using static RonSijm.MediumAPI.OpenApiMigrationExample.IntegrationTest.MigrationHost;
using ExampleProgram = RonSijm.MediumAPI.OpenApiMigrationExample.Program;

namespace RonSijm.MediumAPI.OpenApiMigrationExample.IntegrationTest;

public sealed class OpenApiMigrationTests
{
    [Fact]
    public async Task Independent_mapping_styles_generate_equivalent_real_provider_documents()
    {
        await using var native = Create(MappingStyle.Native);
        await using var medium = Create(MappingStyle.MediumApi);
        await native.StartAsync();
        await medium.StartAsync();

        var expected = ExampleProgram.GetOpenApiJson(native.Services);
        var actual = ExampleProgram.GetOpenApiJson(medium.Services);

        Equivalent(MediumApiContractComparison.CompareOpenApi(expected, actual));
        MediumApiContractComparison.CompareOpenApi(expected, actual).ThrowIfDifferent();
        Assert.Equal(0, Catalog(native).Reads);
        Assert.Equal(0, Catalog(native).Writes);
        Assert.Equal(0, Catalog(medium).Reads);
        Assert.Equal(0, Catalog(medium).Writes);
    }

    [Fact]
    public async Task Raw_routes_and_API_Explorer_descriptions_retain_the_same_selected_contracts()
    {
        await using var native = Create(MappingStyle.Native);
        await using var medium = Create(MappingStyle.MediumApi);
        await native.StartAsync();
        await medium.StartAsync();

        var nativeRoutes = native.GetMediumApiContractSnapshot();
        var mediumRoutes = medium.GetMediumApiContractSnapshot();
        var nativeDescriptions = Descriptions(native);
        var mediumDescriptions = Descriptions(medium);
        Equivalent(MediumApiContractComparison.Compare(nativeRoutes, mediumRoutes));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(nativeDescriptions, mediumDescriptions));
        Assert.Equal(4, mediumRoutes.Routes.Count);
        Assert.Equal(3, mediumDescriptions.Operations.Count);
        Assert.Equal("GetProduct", Assert.Single(mediumRoutes.Routes, route => route.Pattern == "/api/products/{productId:long}").Name);
        Assert.Null(Assert.Single(mediumRoutes.Routes, route => route.Pattern == "/compat/products/{productId:long}").Name);
        Assert.Contains(mediumRoutes.Routes, route => route.Pattern == "/api/internal-status" && route.Name == "GetInternalStatus");
        Assert.DoesNotContain(mediumDescriptions.Operations, operation => operation.Name == "GetInternalStatus");
    }

    [Theory]
    [InlineData(MappingStyle.Native)]
    [InlineData(MappingStyle.MediumApi)]
    public async Task Provider_document_has_real_schemas_typed_outcomes_and_named_and_unnamed_operations(MappingStyle style)
    {
        await using var app = Create(style);
        await app.StartAsync();
        var document = OpenApi(app);
        var paths = Object(document["paths"]);
        var get = GetOperation(document);
        var post = PostOperation(document);
        var alias = Object(Object(paths[AliasPath])["get"]);
        var schemas = Object(Object(document["components"])["schemas"]);

        Assert.StartsWith("3.", document["openapi"]!.GetValue<string>(), StringComparison.Ordinal);
        Assert.Equal(3, paths.Count);
        Assert.False(paths.ContainsKey("/api/internal-status"));
        Assert.Equal("GetProduct", get["operationId"]!.GetValue<string>());
        Assert.Equal("CreateProduct", post["operationId"]!.GetValue<string>());
        Assert.Null(alias["operationId"]);
        Assert.Equal(new[] { "Catalog" }, Array(get["tags"]).Select(tag => tag!.GetValue<string>()));
        Assert.True(schemas.ContainsKey(nameof(ProductResponse)));
        Assert.True(schemas.ContainsKey(nameof(CreateProductRequest)));
        Assert.True(schemas.ContainsKey(nameof(ValidationResponse)));
        Assert.False(schemas.ContainsKey(nameof(InternalStatusResponse)));
        var productProperties = Object(Object(schemas[nameof(ProductResponse)])["properties"]);
        Assert.Equal("integer", productProperties["id"]!["type"]!.GetValue<string>());
        Assert.Equal("int64", productProperties["id"]!["format"]!.GetValue<string>());
        Assert.Equal("string", productProperties["name"]!["type"]!.GetValue<string>());
        Assert.Equal("#/components/schemas/ProductResponse", get["responses"]!["200"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>());
        Assert.NotNull(get["responses"]!["404"]);
        Equivalent(MediumApiContractComparison.CompareJson(get["responses"]!["200"]!.ToJsonString(), alias["responses"]!["200"]!.ToJsonString()));
        Assert.Equal("#/components/schemas/ProductResponse", post["responses"]!["201"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>());
        Assert.Equal("#/components/schemas/ValidationResponse", post["responses"]!["400"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(MappingStyle.Native)]
    [InlineData(MappingStyle.MediumApi)]
    public async Task Provider_retains_binding_sources_requiredness_body_and_the_actual_query_default(MappingStyle style)
    {
        await using var app = Create(style);
        await app.StartAsync();
        var document = OpenApi(app);
        var get = GetOperation(document);
        Assert.Equal(4, Array(get["parameters"]).Count);
        var route = Parameter(get, "productId");
        Assert.Equal("path", route["in"]!.GetValue<string>());
        Assert.True(route["required"]!.GetValue<bool>());
        Assert.Equal("int64", route["schema"]!["format"]!.GetValue<string>());
        var search = Parameter(get, "search");
        Assert.Equal("query", search["in"]!.GetValue<string>());
        Assert.False(search["required"]?.GetValue<bool>() ?? false);
        var header = Parameter(get, "X-Sample-Trace");
        Assert.Equal("header", header["in"]!.GetValue<string>());
        Assert.False(header["required"]?.GetValue<bool>() ?? false);
        var take = Parameter(get, "take");
        Assert.Equal("query", take["in"]!.GetValue<string>());
        Assert.Equal(10, take["schema"]!["default"]!.GetValue<int>());
        Assert.False(take["required"]?.GetValue<bool>() ?? false);
        var body = Object(PostOperation(document)["requestBody"]);
        Assert.True(body["required"]!.GetValue<bool>());
        Assert.Equal("#/components/schemas/CreateProductRequest", body["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>());
    }

    [Fact]
    public async Task Repeated_capture_does_not_dispatch_services_or_add_routes()
    {
        await using var app = Create(MappingStyle.MediumApi);
        await app.StartAsync();
        var routes = app.GetMediumApiContractSnapshot();
        var document = ExampleProgram.GetOpenApiJson(app.Services);
        var again = ExampleProgram.GetOpenApiJson(app.Services);
        var descriptions = Descriptions(app);

        Equivalent(MediumApiContractComparison.CompareOpenApi(document, again));
        Equivalent(MediumApiContractComparison.Compare(routes, app.GetMediumApiContractSnapshot()));
        Assert.Equal(3, descriptions.Operations.Count);
        Assert.Equal(0, Catalog(app).Reads);
        Assert.Equal(0, Catalog(app).Writes);
    }

    [Theory]
    [InlineData("schema")]
    [InlineData("source")]
    [InlineData("required")]
    [InlineData("default")]
    [InlineData("operation")]
    [InlineData("response")]
    [InlineData("body")]
    public async Task Changes_to_copies_of_real_generated_documents_remain_visible(string change)
    {
        await using var app = Create(MappingStyle.MediumApi);
        await app.StartAsync();
        var expected = OpenApi(app);
        var actual = Object(expected.DeepClone());
        var get = GetOperation(actual);
        string expectedPath;
        var kind = MediumApiContractDifferenceKind.Changed;
        switch (change)
        {
            case "schema":
                actual["components"]!["schemas"]!["ProductResponse"]!["properties"]!["name"]!["type"] = "integer";
                expectedPath = "/components/schemas/ProductResponse/properties/name/type";
                break;
            case "source":
                expectedPath = ProductPointer + "/parameters/" + ParameterIndex(get, "search") + "/in";
                Parameter(get, "search")["in"] = "header";
                break;
            case "required":
                expectedPath = ProductPointer + "/parameters/" + ParameterIndex(get, "productId") + "/required";
                Parameter(get, "productId")["required"] = false;
                break;
            case "default":
                expectedPath = ProductPointer + "/parameters/" + ParameterIndex(get, "take") + "/schema/default";
                Parameter(get, "take")["schema"]!["default"] = 11;
                break;
            case "operation":
                get["operationId"] = "ChangedProductOperation";
                expectedPath = ProductPointer + "/operationId";
                break;
            case "response":
                Assert.True(Object(get["responses"]).Remove("404"));
                expectedPath = ProductPointer + "/responses/404";
                kind = MediumApiContractDifferenceKind.Removed;
                break;
            case "body":
                PostOperation(actual)["requestBody"]!["required"] = false;
                expectedPath = "/paths/~1api~1products/post/requestBody/required";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(change));
        }

        var comparison = MediumApiContractComparison.CompareOpenApi(expected.ToJsonString(), actual.ToJsonString());
        Assert.False(comparison.IsEquivalent);
        var difference = Assert.Single(comparison.Differences);
        Assert.Equal(expectedPath, difference.Path);
        Assert.Equal(kind, difference.Kind);
        Assert.Throws<InvalidOperationException>(comparison.ThrowIfDifferent);
    }
}