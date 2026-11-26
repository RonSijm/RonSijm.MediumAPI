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

public sealed class InheritedBindingTests
{
    [Fact]
    public async Task Inherited_non_generic_parser_binds_the_original_long_route_value()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();

        Assert.Equal(new BindingResponse("base-parser", "2147483648"), await client.GetFromJsonAsync<BindingResponse>("/api/inherited/products/2147483648?key=3"));
    }

    [Theory]
    [InlineData("/api/inherited/products/0")]
    [InlineData("/api/inherited/codes/not-valid")]
    public async Task Inherited_parser_failure_remains_a_native_bad_request(string path)
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Closed_generic_base_provider_parser_wins_over_derived_lower_arity_parser()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();

        Assert.Equal(new BindingResponse("base-provider", "abc42"), await client.GetFromJsonAsync<BindingResponse>("/api/inherited/codes/AbC42?key=ignored"));
    }

    [Fact]
    public async Task Explicit_query_uses_inherited_parser_instead_of_inherited_context_binder()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/inherited/selection?selection=QuErY42");
        request.Headers.Add(GenericInheritedCode.HeaderName, "header-value");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal(new BindingResponse("base-provider", "query42"), await response.Content.ReadFromJsonAsync<BindingResponse>());
    }

    [Fact]
    public async Task Implicit_source_uses_closed_generic_base_context_binder()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/inherited/selection/implicit?key=ignored");
        request.Headers.Add(GenericInheritedCode.HeaderName, "header-value");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal(new BindingResponse("base-context", "header-value"), await response.Content.ReadFromJsonAsync<BindingResponse>());
    }

    [Fact]
    public async Task Inherited_preferred_context_overload_sees_parameter_info_and_route_value_before_derived_fallback()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/inherited/context/sample");
        request.Headers.Add(InheritedContextRequest.HeaderName, "recent");
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal(new BindingResponse("base-preferred", "recent", "request", "sample"), await response.Content.ReadFromJsonAsync<BindingResponse>());
    }

    [Fact]
    public async Task Required_inherited_context_result_rejects_null_without_using_derived_fallback()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/inherited/context/sample");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Optional_inherited_context_parameter_accepts_native_null_result()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();

        Assert.Equal(new BindingResponse("base-preferred", "none"), await client.GetFromJsonAsync<BindingResponse>("/api/inherited/optional-context/sample"));
    }

    private static WebApplication Create() => ExampleProgram.Build(configure: builder =>
    {
        builder.WebHost.UseTestServer();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
    });
}