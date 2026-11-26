using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.BindingMetadataExample.Bindings;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.BindingMetadataExample.Program;

namespace RonSijm.MediumAPI.BindingMetadataExample.IntegrationTest;

// Future regression sources only. No hosts, binding, API Explorer or tests ran for this addition.
public sealed class BindingMetadataTests
{
    [Fact]
    public async Task Custom_route_query_header_and_service_metadata_keep_their_native_sources()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/products/2147483648?id=3&productId=4&page=2&revision=ignored");
        request.Headers.Add(RevisionHeaderAttribute.HeaderName, "revision-a");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal(new ProductResponse(2147483648L, "sample-2147483648", 2, "revision-a"), await response.Content.ReadFromJsonAsync<ProductResponse>());
        Assert.Equal(1, app.Services.GetRequiredService<SampleCatalog>().Reads);
    }

    [Fact]
    public async Task Optional_custom_query_and_header_values_remain_nullable()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var result = await client.GetFromJsonAsync<ProductResponse>("/api/products/42");

        Assert.Equal(new ProductResponse(42, "sample-42", null, null), result);
    }

    [Fact]
    public async Task Api_explorer_reports_runtime_custom_names_without_exposing_service_arguments()
    {
        await using var app = Create();
        await app.StartAsync();
        var provider = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var operation = Assert.Single(provider.GetMediumApiDescriptionSnapshot().Operations, entry => entry.Name == "GetProduct");

        Assert.Equal("api/products/{id:long}", operation.RelativePath);
        Assert.Equal(3, operation.Parameters.Count);
        AssertParameter(operation, "id", "Path", true);
        AssertParameter(operation, "page", "Query", false);
        AssertParameter(operation, RevisionHeaderAttribute.HeaderName, "Header", false);
        Assert.DoesNotContain(operation.Parameters, parameter => parameter.Name is "catalog" or "context" or "cancellationToken");
        Assert.Equal(0, app.Services.GetRequiredService<SampleCatalog>().Reads);
    }

    [Fact]
    public async Task Custom_body_and_header_metadata_preserve_created_payload_and_location()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/products?revision=ignored") { Content = JsonContent.Create(new ProductCreateRequest("created")) };
        request.Headers.Add(RevisionHeaderAttribute.HeaderName, "revision-b");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/products/7", response.Headers.Location?.OriginalString);
        Assert.Equal(new ProductCreatedResponse(7, "created", "revision-b"), await response.Content.ReadFromJsonAsync<ProductCreatedResponse>());
    }

    [Fact]
    public async Task Api_explorer_reports_the_custom_body_and_inferred_typed_response()
    {
        await using var app = Create();
        await app.StartAsync();
        var operation = Assert.Single(app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot().Operations, entry => entry.Name == "CreateProduct");
        var body = Assert.Single(operation.Parameters, parameter => parameter.Source == "Body");

        Assert.Equal("create", body.Name);
        Assert.Equal(typeof(ProductCreateRequest).FullName, body.Type);
        Assert.True(body.IsRequired);
        AssertParameter(operation, RevisionHeaderAttribute.HeaderName, "Header", false);
        Assert.Contains("application/json", operation.RequestContentTypes);
        Assert.Contains(operation.Responses, response => response.StatusCode == 201 && response.Type == typeof(ProductCreatedResponse).FullName);
    }

    [Fact]
    public async Task Custom_explicit_header_metadata_takes_precedence_over_BindAsync()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/selection?selected=query-value&binder=context-value");
        request.Headers.Add(SelectionHeaderAttribute.HeaderName, "header-value");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal(new SelectionResponse("header-value", "header"), await response.Content.ReadFromJsonAsync<SelectionResponse>());
    }

    [Fact]
    public async Task Required_custom_header_does_not_fall_back_to_the_context_binder()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/selection?binder=context-value");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_ordinary_endpoint_retains_its_standard_typed_response()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var result = await client.GetFromJsonAsync<StatusResponse>("/api/status");

        Assert.Equal(new StatusResponse("ready"), result);
    }

    private static void AssertParameter(MediumApiOperationDescription operation, string name, string source, bool required)
    {
        var parameter = Assert.Single(operation.Parameters, item => item.Name == name);
        Assert.Equal(source, parameter.Source);
        Assert.Equal(required, parameter.IsRequired);
    }

    private static WebApplication Create() => ExampleProgram.Build(configure: builder =>
    {
        builder.WebHost.UseTestServer();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
    });
}