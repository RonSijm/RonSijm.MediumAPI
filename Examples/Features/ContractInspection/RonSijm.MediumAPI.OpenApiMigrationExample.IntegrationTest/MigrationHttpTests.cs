using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using static RonSijm.MediumAPI.OpenApiMigrationExample.IntegrationTest.MigrationHost;

namespace RonSijm.MediumAPI.OpenApiMigrationExample.IntegrationTest;

public sealed class MigrationHttpTests
{
    [Theory]
    [InlineData(MappingStyle.Native)]
    [InlineData(MappingStyle.MediumApi)]
    public async Task Read_preserves_route_alias_query_header_payload_and_cache_behavior(MappingStyle style)
    {
        await using var app = Create(style);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/products/17?productId=99&search=needle&take=4");
        request.Headers.Add("X-Sample-Trace", "trace-a");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(new ProductResponse(17, "Sample product", "Synthetic catalog item"), await response.Content.ReadFromJsonAsync<ProductResponse>());
        AssertCacheHeaders(response);
        Assert.Equal(1, Catalog(app).Reads);
        Assert.Equal(17, Catalog(app).LastId);
        Assert.Equal("needle", Catalog(app).LastSearch);
        Assert.Equal("trace-a", Catalog(app).LastTrace);
        Assert.Equal(4, Catalog(app).LastTake);
    }

    [Theory]
    [InlineData(MappingStyle.Native)]
    [InlineData(MappingStyle.MediumApi)]
    public async Task Read_uses_the_declared_query_default_when_inputs_are_omitted(MappingStyle style)
    {
        await using var app = Create(style);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/products/17");
        response.EnsureSuccessStatusCode();

        Assert.Equal(1, Catalog(app).Reads);
        Assert.Equal(10, Catalog(app).LastTake);
        Assert.Null(Catalog(app).LastSearch);
        Assert.Null(Catalog(app).LastTrace);
    }

    [Theory]
    [InlineData(MappingStyle.Native)]
    [InlineData(MappingStyle.MediumApi)]
    public async Task Unnamed_alias_serves_the_same_payload_and_cache_headers(MappingStyle style)
    {
        await using var app = Create(style);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/compat/products/17?search=alias");
        response.EnsureSuccessStatusCode();

        Assert.Equal(new ProductResponse(17, "Sample product", "Synthetic catalog item"), await response.Content.ReadFromJsonAsync<ProductResponse>());
        AssertCacheHeaders(response);
        Assert.Equal("alias", Catalog(app).LastSearch);
        Assert.Equal(17, Catalog(app).LastId);
        Assert.Equal(1, Catalog(app).Reads);
    }

    [Theory]
    [InlineData(MappingStyle.Native)]
    [InlineData(MappingStyle.MediumApi)]
    public async Task Explicit_missing_result_keeps_the_handler_owned_cache_policy(MappingStyle style)
    {
        await using var app = Create(style);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/products/18");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertCacheHeaders(response);
        Assert.Equal(1, Catalog(app).Reads);
    }

    [Theory]
    [InlineData(MappingStyle.Native)]
    [InlineData(MappingStyle.MediumApi)]
    public async Task Invalid_query_binding_skips_the_handler_and_its_headers(MappingStyle style)
    {
        await using var app = Create(style);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/products/17?take=not-an-integer");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, Catalog(app).Reads);
        Assert.Null(response.Headers.CacheControl);
        Assert.Empty(response.Headers.Pragma);
    }

    [Theory]
    [InlineData(MappingStyle.Native)]
    [InlineData(MappingStyle.MediumApi)]
    public async Task Failed_route_constraint_keeps_native_not_found_without_dispatch(MappingStyle style)
    {
        await using var app = Create(style);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/products/not-a-number");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, Catalog(app).Reads);
        Assert.Null(response.Headers.CacheControl);
        Assert.Empty(response.Headers.Pragma);
    }

    [Theory]
    [InlineData(MappingStyle.Native)]
    [InlineData(MappingStyle.MediumApi)]
    public async Task Create_keeps_json_input_typed_created_payload_and_location(MappingStyle style)
    {
        await using var app = Create(style);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest("Created sample", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("/api/products/23", response.Headers.Location?.OriginalString);
        Assert.Equal(new ProductResponse(23, "Created sample", null), await response.Content.ReadFromJsonAsync<ProductResponse>());
        Assert.Equal(1, Catalog(app).Writes);
        Assert.Equal(0, Catalog(app).Reads);
    }

    [Theory]
    [InlineData(MappingStyle.Native)]
    [InlineData(MappingStyle.MediumApi)]
    public async Task Invalid_create_keeps_the_explicit_validation_payload_without_writing(MappingStyle style)
    {
        await using var app = Create(style);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(" ", "synthetic"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(new ValidationResponse("name-required"), await response.Content.ReadFromJsonAsync<ValidationResponse>());
        Assert.Equal(0, Catalog(app).Writes);
    }

    [Theory]
    [InlineData(MappingStyle.Native)]
    [InlineData(MappingStyle.MediumApi)]
    public async Task Malformed_json_retains_native_binding_failure_without_writing(MappingStyle style)
    {
        await using var app = Create(style);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var content = new StringContent("{\"name\":", Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/api/products", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, Catalog(app).Writes);
    }

    [Theory]
    [InlineData(MappingStyle.Native)]
    [InlineData(MappingStyle.MediumApi)]
    public async Task Hidden_endpoint_remains_reachable_without_entering_provider_inventory(MappingStyle style)
    {
        await using var app = Create(style);
        await app.StartAsync();
        using var client = app.GetTestClient();

        Assert.Equal(new InternalStatusResponse("ready"), await client.GetFromJsonAsync<InternalStatusResponse>("/api/internal-status"));
        Assert.False(Object(OpenApi(app)["paths"]).ContainsKey("/api/internal-status"));
        Assert.DoesNotContain(Descriptions(app).Operations, operation => operation.Name == "GetInternalStatus");
        Assert.Contains(app.GetMediumApiContractSnapshot().Routes, route => route.Pattern == "/api/internal-status");
        Assert.Equal(0, Catalog(app).Reads);
        Assert.Equal(0, Catalog(app).Writes);
    }

    private static void AssertCacheHeaders(HttpResponseMessage response)
    {
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Contains(response.Headers.Pragma, header => header.Name == "no-cache");
    }
}