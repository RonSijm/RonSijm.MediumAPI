using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using static RonSijm.MediumAPI.AcceptsExample.IntegrationTest.AcceptsHost;

namespace RonSijm.MediumAPI.AcceptsExample.IntegrationTest;

public sealed class NativeAcceptsTests
{
    [Fact]
    public void Native_consumes_metadata_keeps_schema_defaults_order_and_its_own_mutability_semantics()
    {
        var declaration = new ConsumesAttribute("application/json", "application/vnd.catalog+json", "application/json");
        IAcceptsMetadata metadata = declaration;
        Assert.Null(metadata.RequestType);
        Assert.False(metadata.IsOptional);
        Assert.Equal(new[] { "application/json", "application/vnd.catalog+json", "application/json" }, metadata.ContentTypes);
        declaration.IsOptional = true;
        Assert.True(metadata.IsOptional);
        // Native MVC's mutable public collection is separate from its Minimal API IAcceptsMetadata list.
        declaration.ContentTypes.Clear();
        declaration.ContentTypes.Add("text/plain");
        Assert.Equal(new[] { "text/plain" }, declaration.ContentTypes);
        Assert.Equal(new[] { "application/json", "application/vnd.catalog+json", "application/json" }, metadata.ContentTypes);
        IAcceptsMetadata typed = new ConsumesAttribute(typeof(PublishRequest), "application/json") { IsOptional = true };
        Assert.Equal(typeof(PublishRequest), typed.RequestType);
        Assert.True(typed.IsOptional);
    }

    [Fact]
    public void Native_consumes_validates_media_strings_and_rejects_broad_wildcards_before_mapping()
    {
        Assert.Throws<ArgumentNullException>(() => new ConsumesAttribute((string)null!));
        Assert.Throws<FormatException>(() => new ConsumesAttribute("not a media type"));
        Assert.Throws<InvalidOperationException>(() => new ConsumesAttribute("*/*"));
        Assert.Throws<InvalidOperationException>(() => new ConsumesAttribute("text/*"));
    }

    [Fact]
    public async Task All_ordered_and_effective_accepts_metadata_and_route_contracts_match_independent_native_registration()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        await app.StartAsync();
        await native.StartAsync();
        Assert.Equal(15, Routes(app).Count());
        Assert.Equal(Inventory(native), Inventory(app));
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
        Assert.Equal(0, Probe(app).TotalCalls);
        Assert.Equal(0, Probe(app).FilterCalls);
        Assert.Equal(0, Probe(native).TotalCalls);
    }

    [Fact]
    public async Task Native_consumes_objects_are_forwarded_from_endpoint_namespace_and_registered_filter_classes()
    {
        await using var app = Create();
        await app.StartAsync();
        foreach (var path in new[] { "/requests/class-default", "/requests/grouped/inherited", "/requests/filtered", "/requests/documented", "/aliases/first" })
        {
            Assert.IsType<ConsumesAttribute>(Effective(app, path));
        }

        var layered = Route(app, "/requests/layered").Metadata.GetOrderedMetadata<IAcceptsMetadata>();
        Assert.Equal(2, layered.Count);
        Assert.All(layered, metadata => Assert.IsType<ConsumesAttribute>(metadata));
        Assert.Equal(0, Probe(app).TotalCalls);
        Assert.Equal(0, Probe(app).FilterCalls);
    }

    [Fact]
    public async Task Real_API_Explorer_describes_the_same_media_schemas_requiredness_and_aliases_as_native()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        await app.StartAsync();
        await native.StartAsync();
        var descriptions = Descriptions(app);
        Assert.Equal(15, descriptions.Operations.Count);
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), descriptions));
        var upload = Assert.Single(descriptions.Operations, operation => operation.Name == "Upload");
        Assert.Equal(new[] { "multipart/form-data" }, upload.RequestContentTypes);
        var body = Assert.Single(upload.Parameters);
        Assert.Equal(typeof(UploadDescription).FullName, body.Type);
        Assert.Equal("Body", body.Source);
        Assert.True(body.IsRequired);
        var required = Assert.Single(descriptions.Operations, operation => operation.Name == "RequiredJson");
        Assert.True(Assert.Single(required.Parameters).IsRequired);
        var nullable = Assert.Single(descriptions.Operations, operation => operation.Name == "NullableJson");
        Assert.False(Assert.Single(nullable.Parameters).IsRequired);
        var alias = Assert.Single(descriptions.Operations, operation => operation.Name == "FirstRawAlias");
        Assert.Equal(typeof(void).FullName, Assert.Single(alias.Parameters).Type);
        Assert.False(Assert.Single(alias.Parameters).IsRequired);
        Assert.Equal(0, Probe(app).TotalCalls);
    }

    [Fact]
    public async Task Class_default_precedes_JSON_inference_and_original_method_declarations_follow_inference()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            // Grouped data sources may recreate endpoints on enumeration. Compare identity within one endpoint's metadata collection.
            var inferredRoute = Route(app, "/requests/inferred");
            var inferred = inferredRoute.Metadata.GetOrderedMetadata<IAcceptsMetadata>();
            Assert.Equal(2, inferred.Count);
            Assert.Equal(new[] { "text/plain" }, inferred[0].ContentTypes);
            Assert.Equal(new[] { "application/json" }, inferred[1].ContentTypes);
            Assert.False(inferred[1].IsOptional);
            Assert.Same(inferred[1], inferredRoute.Metadata.GetMetadata<IAcceptsMetadata>());
            var requiredRoute = Route(app, "/requests/required");
            var required = requiredRoute.Metadata.GetOrderedMetadata<IAcceptsMetadata>();
            Assert.Equal(2, required.Count);
            Assert.False(required[0].IsOptional);
            Assert.True(required[1].IsOptional);
            Assert.Same(required[1], requiredRoute.Metadata.GetMetadata<IAcceptsMetadata>());
        }
    }

    [Fact]
    public async Task Class_method_layered_filter_and_later_native_conventions_preserve_their_exact_native_order()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var methodRoute = Route(app, "/requests/method-override");
            var method = methodRoute.Metadata.GetOrderedMetadata<IAcceptsMetadata>();
            Assert.Equal(new[] { "text/plain", "application/octet-stream" }, method.Select(item => Assert.Single(item.ContentTypes)));
            Assert.Same(method[1], methodRoute.Metadata.GetMetadata<IAcceptsMetadata>());
            var layeredRoute = Route(app, "/requests/layered");
            var layered = layeredRoute.Metadata.GetOrderedMetadata<IAcceptsMetadata>();
            Assert.Equal(2, layered.Count);
            Assert.Equal(new[] { "text/plain" }, layered[0].ContentTypes);
            Assert.Equal(new[] { "application/json", "application/vnd.catalog+json" }, layered[1].ContentTypes);
            Assert.Same(layered[1], layeredRoute.Metadata.GetMetadata<IAcceptsMetadata>());
            var filteredRoute = Route(app, "/requests/filtered");
            var filtered = filteredRoute.Metadata.GetOrderedMetadata<IAcceptsMetadata>();
            Assert.Equal(new[] { "application/x-configured-upload", "multipart/form-data" }, filtered.Select(item => Assert.Single(item.ContentTypes)));
            Assert.All(filtered, item => Assert.Equal(typeof(UploadDescription), item.RequestType));
            Assert.Same(filtered[1], filteredRoute.Metadata.GetMetadata<IAcceptsMetadata>());
            var laterRoute = Route(app, "/requests/later");
            var later = laterRoute.Metadata.GetOrderedMetadata<IAcceptsMetadata>();
            Assert.Equal(new[] { "application/json", "text/plain" }, later.Select(item => Assert.Single(item.ContentTypes)));
            Assert.Same(later[1], laterRoute.Metadata.GetMetadata<IAcceptsMetadata>());
        }
    }

    [Theory]
    [InlineData("/requests/inferred", "inferred")]
    [InlineData("/requests/required", "required")]
    [InlineData("/requests/nullable", "nullable")]
    public async Task JSON_body_binding_and_typed_results_are_identical_to_native(string path, string source)
    {
        string? expectedBody = null;
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.PostAsJsonAsync(path, new PublishRequest("Catalog", 7));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync();
            if (native)
            {
                expectedBody = body;
            }
            else
            {
                Assert.Equal(expectedBody, body);
            }

            Assert.Equal(new BodyReply(source, "Catalog", 7), await response.Content.ReadFromJsonAsync<BodyReply>());
            Assert.Equal(1, Probe(app).Calls(source));
        }
    }

    [Theory]
    [InlineData("/requests/inferred")]
    [InlineData("/requests/required")]
    [InlineData("/requests/nullable")]
    public async Task Malformed_JSON_is_still_rejected_by_native_parameter_binding_before_the_handler(string path)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.PostAsync(path, new StringContent("{broken", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }

    [Fact]
    public async Task Optional_accepts_metadata_does_not_make_a_required_body_parameter_optional()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.True(Effective(app, "/requests/required").IsOptional);
            Assert.True(Effective(app, "/requests/nullable").IsOptional);
            using var required = await client.PostAsync("/requests/required", null);
            Assert.Equal(HttpStatusCode.BadRequest, required.StatusCode);
            Assert.Equal(0, Probe(app).TotalCalls);
            using var nullable = await client.PostAsync("/requests/nullable", null);
            Assert.Equal(HttpStatusCode.OK, nullable.StatusCode);
            Assert.Equal(new BodyReply("nullable", null, 0), await nullable.Content.ReadFromJsonAsync<BodyReply>());
            Assert.Equal(1, Probe(app).TotalCalls);
        }
    }

    [Theory]
    [InlineData("/requests/inferred", "text/plain")]
    [InlineData("/requests/required", "text/plain")]
    [InlineData("/requests/upload", "application/json")]
    [InlineData("/requests/filtered", "application/x-configured-upload")]
    [InlineData("/requests/method-override", "text/plain")]
    [InlineData("/requests/later", "application/json")]
    [InlineData("/requests/grouped/inherited", "application/json")]
    [InlineData("/aliases/first", "application/json")]
    public async Task Unsupported_content_type_gets_native_415_before_handlers_or_filters(string path, string contentType)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.PostAsync(path, new StringContent("body", Encoding.UTF8, contentType));
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            Assert.Equal(0, Probe(app).TotalCalls);
            Assert.Equal(0, Probe(app).FilterCalls);
        }
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/vnd.catalog+json")]
    public async Task A_documented_schema_does_not_install_JSON_parsing_for_a_manual_body_handler(string contentType)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.PostAsync("/requests/documented", new StringContent("{broken", Encoding.UTF8, contentType));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new BodyReply("documented", "{broken", 7), await response.Content.ReadFromJsonAsync<BodyReply>());
            Assert.Equal(1, Probe(app).TotalCalls);
        }
    }

    [Theory]
    [InlineData("/requests/upload", 0)]
    [InlineData("/requests/filtered", 1)]
    public async Task Manual_multipart_parsing_remains_the_handler_responsibility_with_native_filter_execution(string path, int filterCalls)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var body = new MultipartFormDataContent("catalog-upload-boundary");
            body.Add(new StringContent("Release notes"), "title");
            body.Add(new ByteArrayContent([1, 2, 3, 4]), "file", "notes.bin");
            using var response = await client.PostAsync(path, body);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new UploadReply("Release notes", "notes.bin", 4), await response.Content.ReadFromJsonAsync<UploadReply>());
            Assert.Equal(1, Probe(app).Calls("upload"));
            Assert.Equal(filterCalls, Probe(app).FilterCalls);
        }
    }

    [Theory]
    [InlineData("application/json", "select-json")]
    [InlineData("text/plain", "select-text")]
    public async Task Native_content_type_routing_selects_between_the_same_path_and_verb(string contentType, string source)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.PostAsync("/requests/select", new StringContent("sample", Encoding.UTF8, contentType));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new BodyReply(source, "sample", 6), await response.Content.ReadFromJsonAsync<BodyReply>());
            Assert.Equal(1, Probe(app).Calls(source));
            Assert.Equal(1, Probe(app).TotalCalls);
        }
    }

    [Fact]
    public async Task Native_selection_rejects_a_content_type_supported_by_neither_same_path_endpoint()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.PostAsync("/requests/select", new StringContent("sample", Encoding.UTF8, "application/xml"));
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }

    [Theory]
    [InlineData("/requests/class-default", "text/plain", "class-default")]
    [InlineData("/requests/method-override", "application/octet-stream", "method-override")]
    [InlineData("/requests/later", "text/plain", "later")]
    [InlineData("/requests/grouped/inherited", "application/x-group-document", "grouped")]
    [InlineData("/requests/layered", "application/vnd.catalog+json", "layered")]
    public async Task Native_effective_class_method_group_and_fluent_media_remain_usable_over_HTTP(string path, string contentType, string source)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.PostAsync(path, new StringContent("sample", Encoding.UTF8, contentType));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new BodyReply(source, "sample", 6), await response.Content.ReadFromJsonAsync<BodyReply>());
            Assert.Equal(1, Probe(app).TotalCalls);
        }
    }

    [Theory]
    [InlineData("/aliases/first", "FirstRawAlias")]
    [InlineData("/aliases/second", "SecondRawAlias")]
    public async Task Explicit_aliases_keep_null_schema_optional_metadata_and_do_not_import_namespace_groups(string path, string name)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var metadata = Effective(app, path);
            Assert.Null(metadata.RequestType);
            Assert.True(metadata.IsOptional);
            Assert.Equal(new[] { "application/octet-stream" }, metadata.ContentTypes);
            Assert.Equal(name, Route(app, path).Metadata.GetMetadata<Microsoft.AspNetCore.Routing.IEndpointNameMetadata>()?.EndpointName);
            Assert.Null(Route(app, path).Metadata.GetMetadata<ITagsMetadata>());
            using var client = app.GetTestClient();
            using var response = await client.PostAsync(path, new ByteArrayContent(Encoding.UTF8.GetBytes("raw")) { Headers = { ContentType = new("application/octet-stream") } });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new BodyReply("alias", "raw", 3), await response.Content.ReadFromJsonAsync<BodyReply>());
            using var empty = await client.PostAsync(path, null);
            Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
            Assert.Equal(new BodyReply("alias", "", 0), await empty.Content.ReadFromJsonAsync<BodyReply>());
        }
    }

    [Fact]
    public async Task Disabled_namespaces_and_manual_declared_patterns_create_no_accidental_routes()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            Assert.DoesNotContain(Routes(app), route => route.RoutePattern.RawText?.Contains("disabled", StringComparison.Ordinal) == true);
            Assert.DoesNotContain(Routes(app), route => route.Metadata.GetOrderedMetadata<IAcceptsMetadata>().Any(metadata => metadata.ContentTypes.Contains("application/x-disabled")));
            using var client = app.GetTestClient();
            foreach (var path in new[] { "/requests/disabled", "/unmapped-alias", "/later" })
            {
                using var response = await client.PostAsync(path, null);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }

            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }
}