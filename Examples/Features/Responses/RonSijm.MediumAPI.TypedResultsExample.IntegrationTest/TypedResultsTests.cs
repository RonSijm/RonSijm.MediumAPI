using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.TypedResultsExample.Program;

namespace RonSijm.MediumAPI.TypedResultsExample.IntegrationTest;

// Future regression source only; these tests were not executed in this pass.
public sealed class TypedResultsTests
{
    [Fact]
    public async Task Product_success_keeps_the_default_query_value_and_typed_json_payload()
    {
        await using var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/products/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(new ProductResponse(1, "Sample notebook", 12.50m, "Plain pages"), await response.Content.ReadFromJsonAsync<ProductResponse>());
    }

    [Theory]
    [InlineData("/api/products/0", HttpStatusCode.BadRequest)]
    [InlineData("/api/products/99", HttpStatusCode.NotFound)]
    public async Task Product_failure_branches_keep_their_empty_status_results(string path, HttpStatusCode expected)
    {
        await using var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Product_query_binding_and_single_result_interface_remain_available()
    {
        await using var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        using var client = app.GetTestClient();
        var product = await client.GetFromJsonAsync<ProductResponse>("/api/products/1?includeDescription=false");
        var status = await client.GetFromJsonAsync<ServiceStatus>("/api/status");

        Assert.NotNull(product);
        Assert.Null(product.Description);
        Assert.Equal(new ServiceStatus("ready"), status);
    }

    [Fact]
    public async Task Handler_result_union_exposes_its_response_metadata_without_manual_response_attributes()
    {
        await using var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        var source = app.Services.GetRequiredService<EndpointDataSource>();
        var endpoint = Assert.Single(source.Endpoints.OfType<RouteEndpoint>(), route => route.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "TypedGetProduct");
        var responses = endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();

        Assert.Equal("/api/products/{id:int}", endpoint.RoutePattern.RawText);
        Assert.Contains(responses, response => response.StatusCode == 200 && response.Type == typeof(ProductResponse) && response.ContentTypes.Contains("application/json", StringComparer.OrdinalIgnoreCase));
        Assert.Contains(responses, response => response.StatusCode == 400 && response.Type == typeof(void));
        Assert.Contains(responses, response => response.StatusCode == 404 && response.Type == typeof(void));
    }
}