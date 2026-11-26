using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.NativeFiltersExample.Program;

namespace RonSijm.MediumAPI.NativeFiltersExample.IntegrationTest;

public sealed class NativeFilterTests
{
    [Fact]
    public async Task Plain_class_method_namespace_and_fluent_filters_keep_the_original_argument_pipeline()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/native/value/4?value=80");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<NativeFilterResponse>();

        Assert.NotNull(payload);
        Assert.Equal(14, payload.Value);
        Assert.Equal(Trace("fluent-group", "namespace", "class", "method", "fluent-endpoint"), payload.Trace);
        Assert.Equal(payload.ScopeId.ToString("D"), Assert.Single(response.Headers.GetValues("X-Native-Scope")));
        Assert.Equal("present", Assert.Single(response.Headers.GetValues("X-Native-Group")));
        Assert.Equal("present", Assert.Single(response.Headers.GetValues("X-Native-Class")));
        Assert.Equal("present", Assert.Single(response.Headers.GetValues("X-Native-Fluent")));
    }

    [Fact]
    public async Task The_same_DI_scope_is_used_by_native_filters_and_handler_and_changes_between_requests()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var firstResponse = await client.GetAsync("/native/value/1");
        using var secondResponse = await client.GetAsync("/native/value/1");
        var first = await firstResponse.Content.ReadFromJsonAsync<NativeFilterResponse>();
        var second = await secondResponse.Content.ReadFromJsonAsync<NativeFilterResponse>();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first.ScopeId, second.ScopeId);
        Assert.Equal(first.Trace, second.Trace);
        Assert.Equal(first.ScopeId.ToString("D"), Assert.Single(firstResponse.Headers.GetValues("X-Native-Scope")));
        Assert.Equal(second.ScopeId.ToString("D"), Assert.Single(secondResponse.Headers.GetValues("X-Native-Scope")));
    }

    [Theory]
    [InlineData("/fluent/value/4", "fluent-endpoint", "X-Native-Fluent")]
    [InlineData("/fluent-group/value/4", "fluent-group", "X-Native-Group")]
    public async Task Plain_filters_can_be_registered_on_native_endpoint_and_group_builders(string path, string trace, string header)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<NativeFilterResponse>();

        Assert.NotNull(payload);
        Assert.Equal(4, payload.Value);
        Assert.Equal(Trace(trace), payload.Trace);
        Assert.Equal("present", Assert.Single(response.Headers.GetValues(header)));
    }

    [Fact]
    public async Task A_manual_alias_keeps_endpoint_filters_without_recreating_namespace_filters_or_default_names()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/aliases/value/4");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<NativeFilterResponse>();

        Assert.NotNull(payload);
        Assert.Equal(14, payload.Value);
        Assert.Equal(Trace("class", "method", "fluent-endpoint"), payload.Trace);
        Assert.False(response.Headers.Contains("X-Native-Group"));
        Assert.False(response.Headers.Contains("X-Native-Scope"));
        Assert.Null(Endpoint(app, "/aliases/value/{value:int}").Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
        Assert.Equal("GetNativeValue", Endpoint(app, "/native/value/{value:int}").Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
    }

    [Fact]
    public async Task Convention_aware_and_plain_filters_mix_without_changing_metadata_or_unwinding_order()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/native/mixed/4");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<NativeFilterResponse>();
        var endpoint = Endpoint(app, "/native/mixed/{value:int}");

        Assert.NotNull(payload);
        Assert.Equal(14, payload.Value);
        Assert.Equal(Trace("fluent-group", "namespace", "configured", "method"), payload.Trace);
        Assert.Equal("present", Assert.Single(response.Headers.GetValues("X-Native-Configured")));
        Assert.Equal("configured", Assert.Single(endpoint.Metadata.GetOrderedMetadata<FilterDeclaration>()).Name);
        Assert.Contains(endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), metadata => metadata.StatusCode == 200 && metadata.Type == typeof(NativeFilterResponse));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Native_short_circuits_keep_their_response_and_declared_filter_headers(bool blocked)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/native/guarded/4");
        if (blocked)
        {
            request.Headers.Add("X-Sample-Block", "true");
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(blocked ? HttpStatusCode.BadRequest : HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("present", Assert.Single(response.Headers.GetValues("X-Native-Gate")));
        if (blocked)
        {
            Assert.Equal(new NativeFilterError("blocked"), await response.Content.ReadFromJsonAsync<NativeFilterError>());
        }
        else
        {
            var payload = await response.Content.ReadFromJsonAsync<NativeFilterResponse>();
            Assert.NotNull(payload);
            Assert.Equal(4, payload.Value);
            Assert.Equal(Trace("fluent-group", "namespace", "gate"), payload.Trace);
        }
    }

    private static WebApplication CreateHost() => ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
    private static IEnumerable<string> Trace(params string[] entries) => entries.Select(entry => entry + ":enter").Append("handler").Concat(entries.Reverse().Select(entry => entry + ":exit"));
    private static RouteEndpoint Endpoint(WebApplication app, string pattern) => Assert.Single(app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>(), endpoint => endpoint.RoutePattern.RawText == pattern);
}