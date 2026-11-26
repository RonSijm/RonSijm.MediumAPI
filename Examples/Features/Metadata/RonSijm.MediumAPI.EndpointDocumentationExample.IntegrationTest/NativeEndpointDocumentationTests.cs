using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;
using static RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.EndpointDocumentationHost;
using ExampleProgram = RonSijm.MediumAPI.EndpointDocumentationExample.Program;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest;

public sealed class NativeEndpointDocumentationTests
{
    [Theory]
    [InlineData(DocumentationGroups.Public)]
    [InlineData(DocumentationGroups.Admin)]
    public async Task Class_declarations_generate_complete_provider_documents_equivalent_to_independent_native_mappings(string documentName)
    {
        await using var native = Create(native: true);
        await using var generated = Create();
        await native.StartAsync();
        await generated.StartAsync();
        var expected = ExampleProgram.GetOpenApiJson(native.Services, documentName);
        var actual = ExampleProgram.GetOpenApiJson(generated.Services, documentName);
        var comparison = MediumApiContractComparison.CompareOpenApi(expected, actual);
        Equivalent(comparison);
        comparison.ThrowIfDifferent();
        var expectedPaths = documentName == DocumentationGroups.Public ? new[] { "/documentation/class", "/documentation/configured", "/documentation/inherited", "/shared" } : new[] { "/documentation/admin", "/documentation/method", "/shared" };
        Assert.Equal(expectedPaths, Paths(OpenApi(native, documentName)));
        Assert.Equal(expectedPaths, Paths(OpenApi(generated, documentName)));
        Assert.Equal(0, Probe(native).Calls);
        Assert.Equal(0, Probe(generated).Calls);
    }

    [Fact]
    public async Task Route_and_API_Explorer_snapshots_preserve_class_tags_document_groups_and_exclusion()
    {
        await using var native = Create(native: true);
        await using var generated = Create();
        await native.StartAsync();
        await generated.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.Equal(MetadataInventory(native), MetadataInventory(generated));
        Assert.Equal(8, Routes(generated).Count());
        var operations = Descriptions(generated).Operations;
        Assert.Equal(7, operations.Count);
        Assert.Equal(DocumentationGroups.Public, Assert.Single(operations, operation => operation.Name == "GetClassPublic").GroupName);
        Assert.Equal(DocumentationGroups.Admin, Assert.Single(operations, operation => operation.Name == "GetClassAdmin").GroupName);
        Assert.Equal(DocumentationGroups.Admin, Assert.Single(operations, operation => operation.Name == "GetMethodOverride").GroupName);
        Assert.Equal(DocumentationGroups.Public, Assert.Single(operations, operation => operation.Name == "GetConfiguredOverride").GroupName);
        Assert.Equal(DocumentationGroups.Public, Assert.Single(operations, operation => operation.Name == "GetInheritedDefaults").GroupName);
        Assert.Null(Assert.Single(operations, operation => operation.Name == "GetShared").GroupName);
        Assert.Equal(DocumentationGroups.Unmatched, Assert.Single(operations, operation => operation.Name == "GetUnmatched").GroupName);
        Assert.DoesNotContain(operations, operation => operation.Name == "GetHidden");
    }

    [Theory]
    [InlineData("/documentation/class", DocumentationGroups.Public, DocumentationTags.Catalog + "," + DocumentationTags.Read, false)]
    [InlineData("/documentation/admin", DocumentationGroups.Admin, DocumentationTags.Admin, false)]
    [InlineData("/documentation/method", DocumentationGroups.Admin, DocumentationTags.Method, false)]
    [InlineData("/documentation/configured", DocumentationGroups.Public, DocumentationTags.Configured + "," + DocumentationTags.Read, false)]
    [InlineData("/documentation/inherited", DocumentationGroups.Public, DocumentationTags.Group, false)]
    [InlineData("/documentation/hidden", DocumentationGroups.Admin, DocumentationTags.Hidden, true)]
    [InlineData("/documentation/unmatched", DocumentationGroups.Unmatched, DocumentationTags.Unmatched, false)]
    [InlineData("/shared", null, DocumentationTags.Shared + "," + DocumentationTags.Read, false)]
    public async Task Effective_native_documentation_metadata_keeps_class_method_and_Configure_selection(string path, string? documentName, string tags, bool excluded)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var metadata = Route(app, path).Metadata;
            Assert.Equal(documentName, metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName);
            Assert.Equal(tags.Split(','), metadata.GetMetadata<ITagsMetadata>()!.Tags);
            Assert.Equal(excluded, metadata.GetMetadata<IExcludeFromDescriptionMetadata>()?.ExcludeFromDescription ?? false);
            var descriptions = ApiDescriptions(app).Where(description => "/" + description.RelativePath == path).ToArray();
            if (excluded)
            {
                Assert.Empty(descriptions);
            }
            else
            {
                var description = Assert.Single(descriptions);
                Assert.Equal(documentName, description.GroupName);
                var describedMetadata = new Microsoft.AspNetCore.Http.EndpointMetadataCollection(description.ActionDescriptor.EndpointMetadata);
                Assert.Equal(tags.Split(','), describedMetadata.GetMetadata<ITagsMetadata>()!.Tags);
            }
        }
    }

    [Fact]
    public async Task Ordered_metadata_retains_group_class_method_and_Configure_stages_without_merging_tags()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var method = Route(app, "/documentation/method").Metadata;
            Assert.Equal(new[] { DocumentationGroups.Public, DocumentationGroups.Public, DocumentationGroups.Admin }, method.GetOrderedMetadata<IEndpointGroupNameMetadata>().Select(value => value.EndpointGroupName));
            Assert.Equal(new[] { DocumentationTags.Group, DocumentationTags.Catalog + "," + DocumentationTags.Read, DocumentationTags.Method }, method.GetOrderedMetadata<ITagsMetadata>().Select(value => string.Join(",", value.Tags)));
            Assert.Equal(new[] { DocumentationTags.Method }, method.GetMetadata<ITagsMetadata>()!.Tags);
            var configured = Route(app, "/documentation/configured").Metadata;
            Assert.Equal(new[] { DocumentationGroups.Public, DocumentationGroups.Admin, DocumentationGroups.Public }, configured.GetOrderedMetadata<IEndpointGroupNameMetadata>().Select(value => value.EndpointGroupName));
            Assert.Equal(new[] { DocumentationTags.Group, DocumentationTags.Admin, DocumentationTags.Configured + "," + DocumentationTags.Read }, configured.GetOrderedMetadata<ITagsMetadata>().Select(value => string.Join(",", value.Tags)));
            Assert.Same(configured.GetOrderedMetadata<ITagsMetadata>()[^1], configured.GetMetadata<ITagsMetadata>());
        }
    }

    [Theory]
    [InlineData("/documentation/class", "public")]
    [InlineData("/documentation/admin", "admin")]
    [InlineData("/documentation/method", "method")]
    [InlineData("/documentation/configured", "configured")]
    [InlineData("/documentation/inherited", "inherited")]
    [InlineData("/documentation/hidden", "hidden")]
    [InlineData("/documentation/unmatched", "unmatched")]
    [InlineData("/shared", "shared")]
    public async Task Class_documentation_choices_preserve_HTTP_reachability_and_typed_payloads(string path, string kind)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new DocumentationResponse(kind), await Read(client, path));
            Assert.Equal(1, Probe(app).Calls);
        }
    }

    [Theory]
    [InlineData(DocumentationGroups.Public)]
    [InlineData(DocumentationGroups.Admin)]
    public async Task Provider_operations_retain_typed_response_schema_default_names_and_ordered_class_tags(string documentName)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var document = OpenApi(app, documentName);
            Assert.StartsWith("3.", document["openapi"]!.GetValue<string>(), StringComparison.Ordinal);
            var schemas = Object(Object(document["components"])["schemas"]);
            Assert.Equal("string", schemas[nameof(DocumentationResponse)]!["properties"]!["kind"]!["type"]!.GetValue<string>());
            foreach (var path in Paths(document))
            {
                var operation = Object(document["paths"]![path]!["get"]);
                var expected = path switch
                {
                    "/documentation/class" => ("GetClassPublic", new[] { DocumentationTags.Catalog, DocumentationTags.Read }),
                    "/documentation/admin" => ("GetClassAdmin", new[] { DocumentationTags.Admin }),
                    "/documentation/method" => ("GetMethodOverride", new[] { DocumentationTags.Method }),
                    "/documentation/configured" => ("GetConfiguredOverride", new[] { DocumentationTags.Configured, DocumentationTags.Read }),
                    "/documentation/inherited" => ("GetInheritedDefaults", new[] { DocumentationTags.Group }),
                    "/shared" => ("GetShared", new[] { DocumentationTags.Shared, DocumentationTags.Read }),
                    _ => throw new InvalidOperationException("Unexpected documented route.")
                };
                Assert.Equal(expected.Item1, operation["operationId"]!.GetValue<string>());
                Assert.Equal(expected.Item2, Assert.IsType<JsonArray>(operation["tags"]).Select(tag => tag!.GetValue<string>()));
                Assert.Equal("#/components/schemas/DocumentationResponse", operation["responses"]!["200"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>());
            }
        }
    }

    [Fact]
    public async Task Class_exclusion_and_unmatched_document_names_have_distinct_description_boundaries()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            Assert.NotNull(Route(app, "/documentation/hidden").Metadata.GetMetadata<IExcludeFromDescriptionMetadata>());
            Assert.DoesNotContain(Descriptions(app).Operations, operation => operation.Name == "GetHidden");
            Assert.Equal(DocumentationGroups.Unmatched, Assert.Single(Descriptions(app).Operations, operation => operation.Name == "GetUnmatched").GroupName);
            foreach (var documentName in new[] { DocumentationGroups.Public, DocumentationGroups.Admin })
            {
                var paths = Paths(OpenApi(app, documentName));
                Assert.DoesNotContain("/documentation/hidden", paths);
                Assert.DoesNotContain("/documentation/unmatched", paths);
                Assert.Contains("/shared", paths);
            }

            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Repeated_document_capture_does_not_dispatch_handlers_or_change_route_contracts()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var routes = app.GetMediumApiContractSnapshot();
            foreach (var documentName in new[] { DocumentationGroups.Public, DocumentationGroups.Admin })
            {
                var document = ExampleProgram.GetOpenApiJson(app.Services, documentName);
                Equivalent(MediumApiContractComparison.CompareOpenApi(document, ExampleProgram.GetOpenApiJson(app.Services, documentName)));
            }

            Equivalent(MediumApiContractComparison.Compare(routes, app.GetMediumApiContractSnapshot()));
            Assert.Equal(8, Routes(app).Count());
            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Class_document_names_do_not_register_provider_documents_implicitly()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var registered = app.Services.GetRequiredService<IOptions<SwaggerGeneratorOptions>>().Value.SwaggerDocs.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();
            Assert.Equal(new[] { DocumentationGroups.Admin, DocumentationGroups.Public }, registered);
            Assert.DoesNotContain(DocumentationGroups.Unmatched, registered);
        }
    }

    [Theory]
    [InlineData(DocumentationGroups.Public)]
    [InlineData(DocumentationGroups.Admin)]
    public async Task Real_document_tag_array_changes_remain_visible_to_strict_contract_comparison(string documentName)
    {
        await using var app = Create();
        await app.StartAsync();
        var expected = OpenApi(app, documentName);
        var changed = Object(expected.DeepClone());
        changed["paths"]!["/shared"]!["get"]!["tags"]![0] = "Changed shared tag";
        var comparison = MediumApiContractComparison.CompareOpenApi(expected.ToJsonString(), changed.ToJsonString());
        Assert.False(comparison.IsEquivalent);
        var difference = Assert.Single(comparison.Differences);
        Assert.Equal(MediumApiContractDifferenceKind.Changed, difference.Kind);
        Assert.Equal("/paths/~1shared/get/tags/0", difference.Path);
        Assert.Throws<InvalidOperationException>(comparison.ThrowIfDifferent);
    }
}