using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.CustomBindingExample.Bindings;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.CustomBindingExample.Program;

namespace RonSijm.MediumAPI.CustomBindingExample.IntegrationTest;

// HTTP regression cases use native binding through the original-handler adapter.
public sealed class CustomBindingTests
{
    [Fact]
    public async Task Inherited_route_alias_binds_a_reference_parser_without_narrowing_the_long_token()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var result = await client.GetFromJsonAsync<BindingResponse>("/api/products/2147483648/details?key=3&productId=3");

        Assert.Equal(new BindingResponse("route", "2147483648"), result);
    }

    [Fact]
    public async Task A_failed_reference_parser_uses_the_framework_binding_failure()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/products/0/details");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Explicit_IParsable_implementation_parses_the_route_value()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var result = await client.GetFromJsonAsync<BindingResponse>("/api/codes/AbC42?code=ignored");

        Assert.Equal(new BindingResponse("route", "abc42"), result);
    }

    [Fact]
    public async Task Explicit_IParsable_failure_remains_a_native_bad_request()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/codes/not-valid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Struct_context_binder_sees_its_parameter_and_a_route_token_without_a_separate_handler_parameter()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/context/sample");
        request.Headers.Add(WindowRequest.HeaderName, "recent");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal(new BindingResponse("context", "recent", "window", "sample"), await response.Content.ReadFromJsonAsync<BindingResponse>());
    }

    [Fact]
    public async Task Nullable_struct_parameter_accepts_the_context_binders_null_result()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        var result = await client.GetFromJsonAsync<BindingResponse>("/api/context/sample");

        Assert.Equal(new BindingResponse("context", "none"), result);
    }

    [Fact]
    public async Task Explicit_IBindable_interface_uses_the_context_and_parameter_name()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/request-token");
        request.Headers.Add(RequestToken.HeaderName, "synthetic-token");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal(new BindingResponse("context", "synthetic-token", "token"), await response.Content.ReadFromJsonAsync<BindingResponse>());
    }

    [Fact]
    public async Task Required_class_context_parameter_rejects_a_null_binding_result()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/request-token");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Explicit_query_source_takes_precedence_over_a_context_binder_on_the_same_type()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/selection?selection=query-value");
        request.Headers.Add(SelectionKey.HeaderName, "header-value");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal(new BindingResponse("query", "query-value"), await response.Content.ReadFromJsonAsync<BindingResponse>());
    }

    [Fact]
    public async Task Implicit_source_uses_context_binding_before_string_parsing()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/selection/implicit?selected=query-value");
        request.Headers.Add(SelectionKey.HeaderName, "header-value");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal(new BindingResponse("context", "header-value"), await response.Content.ReadFromJsonAsync<BindingResponse>());
    }

    private static WebApplication Create() => ExampleProgram.Build(configure: builder =>
    {
        builder.WebHost.UseTestServer();
        // The expected 400 is independent of a developer machine's environment.
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
    });
}