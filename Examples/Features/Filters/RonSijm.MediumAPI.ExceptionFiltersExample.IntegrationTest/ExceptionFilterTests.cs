using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.ExceptionFiltersExample.Endpoints.Disabled;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.ExceptionFiltersExample.Program;

namespace RonSijm.MediumAPI.ExceptionFiltersExample.IntegrationTest;

// Future regression source only. No tests, hosts or examples were executed in this pass.
public sealed class ExceptionFilterTests
{
    [Fact]
    public async Task Successful_typed_handler_keeps_its_payload_and_status()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/products/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new ProductResponse(1, "Sample notebook"), await response.Content.ReadFromJsonAsync<ProductResponse>());
    }

    [Theory]
    [InlineData("/api/products/2", HttpStatusCode.NotFound)]
    [InlineData("/api/products/3", HttpStatusCode.Conflict)]
    [InlineData("/api/class/2", HttpStatusCode.NotFound)]
    [InlineData("/api/class/3", HttpStatusCode.Conflict)]
    [InlineData("/api/method/2", HttpStatusCode.NotFound)]
    [InlineData("/api/method/3", HttpStatusCode.Conflict)]
    [InlineData("/api/fluent/2", HttpStatusCode.NotFound)]
    [InlineData("/api/fluent/3", HttpStatusCode.Conflict)]
    public async Task Known_legacy_exceptions_are_translated_for_each_registration_style(string path, HttpStatusCode expected)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.NotFound)
        {
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            return;
        }

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(409, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("product-conflict", body.RootElement.GetProperty("code").GetString());
        Assert.Equal("The product changed. Reload and try again.", body.RootElement.GetProperty("title").GetString());
        Assert.False(body.RootElement.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task Filter_configuration_declares_failures_alongside_the_typed_success()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        foreach (var prefix in new[] { "products", "class", "method", "fluent" })
        {
            var responses = Endpoint(app, "/api/" + prefix + "/{id:int}").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
            Assert.Contains(responses, response => response.StatusCode == 200 && response.Type == typeof(ProductResponse));
            Assert.Contains(responses, response => response.StatusCode == 404 && response.Type == typeof(void));
            Assert.Contains(responses, response => response.StatusCode == 409 && response.Type == typeof(ProblemDetails) && response.ContentTypes.Contains("application/problem+json", StringComparer.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task Native_filter_DI_and_registration_order_use_one_request_scope_and_reverse_unwinding()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var first = await client.GetFromJsonAsync<PipelineResponse>("/api/pipeline");
        var second = await client.GetFromJsonAsync<PipelineResponse>("/api/pipeline");
        var entryOrder = new[] { "fluent-group", "declared-group", "class", "method", "fluent-endpoint" };
        var expected = entryOrder.Select(name => name + ":enter").Append("handler").Concat(entryOrder.Reverse().Select(name => name + ":exit"));

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(expected, first.Trace);
        Assert.Equal(first.Trace, second.Trace);
        Assert.NotEqual(first.ScopeId, second.ScopeId);
        Assert.Equal(entryOrder, Endpoint(app, "/api/pipeline/").Metadata.GetOrderedMetadata<FilterDeclaration>().Select(declaration => declaration.Name));
    }

    [Fact]
    public async Task Disabled_namespace_and_typed_mount_skip_filter_configuration_and_native_routes()
    {
        await using var app = CreateHost();
        // MustStayDisabledFilter.Configure throws if either disabled registration runs it.
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/disabled/native");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>(), endpoint => endpoint.RoutePattern.RawText?.Contains("/disabled/", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Unexpected_exception_propagates_without_becoming_an_application_problem()
    {
        var failure = new InvalidOperationException("Synthetic unexpected failure.");
        var filter = new ProductExceptionFilter();
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext());

        var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => filter.InvokeAsync(context, _ => ValueTask.FromException<object?>(failure)).AsTask());

        Assert.Same(failure, observed);
    }

    [Fact]
    public async Task Cancellation_propagates_without_becoming_an_application_problem()
    {
        var failure = new OperationCanceledException();
        var filter = new ProductExceptionFilter();
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext());

        var observed = await Assert.ThrowsAsync<OperationCanceledException>(() => filter.InvokeAsync(context, _ => ValueTask.FromException<object?>(failure)).AsTask());

        Assert.Same(failure, observed);
    }

    private static WebApplication CreateHost() => ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());

    private static RouteEndpoint Endpoint(WebApplication app, string pattern) => Assert.Single(app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>(), endpoint => endpoint.RoutePattern.RawText == pattern);
}