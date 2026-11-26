using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;
using static RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest.DocumentGroupsHost;
using ExampleProgram = RonSijm.MediumAPI.DocumentGroupsExample.Program;

namespace RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest;

public sealed class DocumentGroupContractsTests
{
    [Theory]
    [InlineData(DocumentGroups.Public)]
    [InlineData(DocumentGroups.Admin)]
    public async Task Independent_mappings_generate_complete_equivalent_provider_documents(string documentName)
    {
        await using var native = Create(native: true);
        await using var generated = Create();
        await native.StartAsync();
        await generated.StartAsync();
        var expected = ExampleProgram.GetOpenApiJson(native.Services, documentName);
        var actual = ExampleProgram.GetOpenApiJson(generated.Services, documentName);
        Equivalent(MediumApiContractComparison.CompareOpenApi(expected, actual));
        MediumApiContractComparison.CompareOpenApi(expected, actual).ThrowIfDifferent();
        var expectedPaths = documentName == DocumentGroups.Public ? new[] { "/catalog/child/value", "/catalog/value", "/shared" } : new[] { "/catalog/admin/value", "/catalog/detail", "/shared" };
        Assert.Equal(expectedPaths, Paths(OpenApi(native, documentName)));
        Assert.Equal(expectedPaths, Paths(OpenApi(generated, documentName)));
        Assert.Equal(0, Probe(native).Calls);
        Assert.Equal(0, Probe(generated).Calls);
    }

    [Fact]
    public async Task Raw_and_API_Explorer_inventories_preserve_groups_hidden_routes_and_unmatched_descriptions()
    {
        await using var native = Create(native: true);
        await using var generated = Create();
        await native.StartAsync();
        await generated.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.Equal(GroupInventory(native), GroupInventory(generated));
        Assert.Equal(7, Routes(generated).Count());
        var operations = Descriptions(generated).Operations;
        Assert.Equal(6, operations.Count);
        Assert.Equal(DocumentGroups.Public, Assert.Single(operations, operation => operation.Name == "GetCatalogValue").GroupName);
        Assert.Equal(DocumentGroups.Admin, Assert.Single(operations, operation => operation.Name == "GetCatalogDetail").GroupName);
        Assert.Equal(DocumentGroups.Admin, Assert.Single(operations, operation => operation.Name == "GetAdminValue").GroupName);
        Assert.Equal(DocumentGroups.Public, Assert.Single(operations, operation => operation.Name == "GetChildValue").GroupName);
        Assert.Null(Assert.Single(operations, operation => operation.Name == "GetSharedValue").GroupName);
        Assert.Equal(DocumentGroups.Unmatched, Assert.Single(operations, operation => operation.Name == "GetExperimentalValue").GroupName);
        Assert.DoesNotContain(operations, operation => operation.Name == "GetHiddenCatalogValue");
        Assert.Equal(DocumentGroups.Public, Route(generated, "/catalog/hidden").Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
    }

    [Theory]
    [InlineData(DocumentGroups.Public)]
    [InlineData(DocumentGroups.Admin)]
    public async Task Each_real_document_retains_typed_response_schemas_tags_and_default_operation_names(string documentName)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var document = OpenApi(app, documentName);
            Assert.StartsWith("3.", document["openapi"]!.GetValue<string>(), StringComparison.Ordinal);
            var schemas = Object(Object(document["components"])["schemas"]);
            var properties = Object(Object(schemas[nameof(DocumentResponse)])["properties"]);
            Assert.Equal("string", properties["kind"]!["type"]!.GetValue<string>());
            foreach (var path in Paths(document))
            {
                var operation = Object(Object(Object(document["paths"])[path])["get"]);
                var expectedName = path switch
                {
                    "/catalog/value" => "GetCatalogValue",
                    "/catalog/child/value" => "GetChildValue",
                    "/catalog/admin/value" => "GetAdminValue",
                    "/catalog/detail" => "GetCatalogDetail",
                    "/shared" => "GetSharedValue",
                    _ => throw new InvalidOperationException("Unexpected documented route.")
                };
                Assert.Equal(expectedName, operation["operationId"]!.GetValue<string>());
                Assert.Equal(new[] { DocumentTags.Endpoints }, Assert.IsType<JsonArray>(operation["tags"]).Select(tag => tag!.GetValue<string>()));
                Assert.Equal("#/components/schemas/DocumentResponse", operation["responses"]!["200"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>());
            }
        }
    }

    [Theory]
    [InlineData("/catalog/value", "catalog", DocumentGroups.Public)]
    [InlineData("/catalog/detail", "detail", DocumentGroups.Admin)]
    [InlineData("/catalog/admin/value", "admin", DocumentGroups.Admin)]
    [InlineData("/catalog/child/value", "child", DocumentGroups.Public)]
    [InlineData("/catalog/hidden", "hidden", DocumentGroups.Public)]
    [InlineData("/shared", "shared", null)]
    [InlineData("/experimental/value", "experimental", DocumentGroups.Unmatched)]
    public async Task Documentation_group_selection_preserves_HTTP_reachability_and_handler_payloads(string path, string kind, string? groupName)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            Assert.Equal(groupName, Route(app, path).Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName);
            using var client = app.GetTestClient();
            Assert.Equal(new DocumentResponse(kind), await Read(client, path));
            Assert.Equal(1, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Capturing_both_documents_repeatedly_does_not_dispatch_handlers_or_change_exposure()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var routes = app.GetMediumApiContractSnapshot();
            foreach (var documentName in new[] { DocumentGroups.Public, DocumentGroups.Admin })
            {
                var expected = ExampleProgram.GetOpenApiJson(app.Services, documentName);
                Equivalent(MediumApiContractComparison.CompareOpenApi(expected, ExampleProgram.GetOpenApiJson(app.Services, documentName)));
            }

            Equivalent(MediumApiContractComparison.Compare(routes, app.GetMediumApiContractSnapshot()));
            Assert.Equal(7, Routes(app).Count());
            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task A_group_name_does_not_automatically_register_a_provider_document()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var configuredDocuments = app.Services.GetRequiredService<IOptions<SwaggerGeneratorOptions>>().Value.SwaggerDocs.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            Assert.Equal(new[] { DocumentGroups.Admin, DocumentGroups.Public }, configuredDocuments);
            Assert.DoesNotContain(DocumentGroups.Unmatched, configuredDocuments);
            Assert.Equal(DocumentGroups.Unmatched, Route(app, "/experimental/value").Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
            using var client = app.GetTestClient();
            Assert.Equal(new DocumentResponse("experimental"), await Read(client, "/experimental/value"));
        }
    }

    [Theory]
    [InlineData(DocumentGroups.Public)]
    [InlineData(DocumentGroups.Admin)]
    public async Task Changes_to_real_selected_documents_remain_visible_to_contract_comparison(string documentName)
    {
        await using var app = Create();
        await app.StartAsync();
        var expected = OpenApi(app, documentName);
        var changed = Object(expected.DeepClone());
        changed["paths"]!["/shared"]!["get"]!["operationId"] = "ChangedSharedOperation";
        var comparison = MediumApiContractComparison.CompareOpenApi(expected.ToJsonString(), changed.ToJsonString());
        Assert.False(comparison.IsEquivalent);
        var difference = Assert.Single(comparison.Differences);
        Assert.Equal(MediumApiContractDifferenceKind.Changed, difference.Kind);
        Assert.Equal("/paths/~1shared/get/operationId", difference.Path);
        Assert.Throws<InvalidOperationException>(comparison.ThrowIfDifferent);
    }
}