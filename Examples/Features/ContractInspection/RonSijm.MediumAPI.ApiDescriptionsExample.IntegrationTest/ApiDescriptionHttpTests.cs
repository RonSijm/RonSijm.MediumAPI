using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.ApiDescriptionsExample.Program;

namespace RonSijm.MediumAPI.ApiDescriptionsExample.IntegrationTest;

// Future regression sources only. No hosts, API Explorer providers or cases ran for this addition.
public sealed class ApiDescriptionHttpTests
{
    [Fact]
    public async Task Snapshot_retains_described_route_alias_query_header_and_requiredness()
    {
        await using var app = Create();
        await app.StartAsync();
        var snapshot = Capture(app);
        var product = Assert.Single(snapshot.Operations, operation => operation.Name == "GetProduct");

        Assert.Equal("catalog", product.GroupName);
        Assert.Equal("GET", product.HttpMethod);
        Assert.Equal("api/products/{id:long}", product.RelativePath);
        Assert.Equal(3, product.Parameters.Count);
        AssertParameter(product, "id", "Path", typeof(long), true);
        AssertParameter(product, "search", "Query", typeof(string), false);
        AssertParameter(product, "X-Sample-Revision", "Header", typeof(string), false);
        Assert.DoesNotContain(product.Parameters, parameter => parameter.Name is "catalog" or "context" or "cancellationToken");
    }

    [Fact]
    public async Task Snapshot_retains_body_and_typed_created_response_descriptions()
    {
        await using var app = Create();
        await app.StartAsync();
        var created = Assert.Single(Capture(app).Operations, operation => operation.Name == "CreateProduct");

        Assert.Equal("POST", created.HttpMethod);
        Assert.Equal("api/products", created.RelativePath);
        AssertParameter(created, "create", "Body", typeof(ProductCreateRequest), true);
        AssertParameter(created, "X-Sample-Request", "Header", typeof(string), false);
        Assert.Contains("application/json", created.RequestContentTypes);
        var response = Assert.Single(created.Responses, item => item.StatusCode == 201);
        Assert.False(response.IsDefaultResponse);
        Assert.Equal(typeof(ProductCreatedResponse).FullName, response.Type);
        Assert.Contains("application/json", response.ContentTypes);
    }

    [Fact]
    public async Task An_undocumented_endpoint_can_still_exist_in_the_raw_route_inventory_and_serve_requests()
    {
        await using var app = Create();
        await app.StartAsync();
        var descriptions = Capture(app);
        var routes = app.GetMediumApiContractSnapshot();
        using var client = app.GetTestClient();
        var hidden = await client.GetFromJsonAsync<StatusResponse>("/api/internal-status");

        Assert.Equal(new StatusResponse("internal-ready"), hidden);
        Assert.DoesNotContain(descriptions.Operations, operation => operation.Name == "GetInternalStatus");
        Assert.Equal(3, descriptions.Operations.Count);
        Assert.Contains(routes.Routes, route => route.Name == "GetInternalStatus" && route.Pattern == "/api/internal-status");
        Assert.Equal(4, routes.Routes.Count);
    }

    [Fact]
    public async Task Description_capture_does_not_dispatch_the_handler_and_requests_keep_their_values()
    {
        await using var app = Create();
        await app.StartAsync();
        var catalog = app.Services.GetRequiredService<SampleCatalog>();
        var before = Capture(app);
        var again = Capture(app);
        Assert.Equal(0, catalog.Reads);
        Assert.True(MediumApiContractComparison.CompareApiDescriptions(before, again).IsEquivalent);
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/products/123?search=sample");
        request.Headers.Add("X-Sample-Revision", "rev-1");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal(new ProductDetailsResponse(123, "sample-123", "sample", "rev-1"), await response.Content.ReadFromJsonAsync<ProductDetailsResponse>());
        Assert.Equal(1, catalog.Reads);
    }

    [Fact]
    public async Task Typed_creation_keeps_its_body_header_status_and_location()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/products") { Content = JsonContent.Create(new ProductCreateRequest("created")) };
        request.Headers.Add("X-Sample-Request", "request-a");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/products/7", response.Headers.Location?.OriginalString);
        Assert.Equal(new ProductCreatedResponse(7, "created", "request-a"), await response.Content.ReadFromJsonAsync<ProductCreatedResponse>());
    }

    private static void AssertParameter(MediumApiOperationDescription operation, string name, string source, Type type, bool required)
    {
        var parameter = Assert.Single(operation.Parameters, item => item.Name == name);
        Assert.Equal(source, parameter.Source);
        Assert.Equal(type.FullName, parameter.Type);
        Assert.Equal(required, parameter.IsRequired);
    }

    private static MediumApiDescriptionSnapshot Capture(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();

    private static WebApplication Create() => ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
}