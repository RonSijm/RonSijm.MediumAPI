using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.RouteBindingExample.Program;

namespace RonSijm.MediumAPI.RouteBindingExample.IntegrationTest;

public sealed class InheritedBindingTests
{
    [Fact]
    public async Task Native_fluent_conventions_keep_inherited_routes_and_endpoint_metadata()
    {
        await using var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        var endpoint = Assert.Single(((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>(),
            candidate => candidate.RoutePattern.RawText == "/tenants/{tenant}/api/{customerId:long}/items/{id:int}");

        Assert.Equal(new[] { "Inherited routes" }, endpoint.Metadata.GetMetadata<ITagsMetadata>()?.Tags);
        Assert.Equal("RouteBinding", endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName);
        Assert.Equal("Customer route bindings", endpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>()?.Summary);
        Assert.Equal("Items under the inherited customer route", endpoint.Metadata.GetMetadata<IEndpointDescriptionMetadata>()?.Description);
        using var client = app.GetTestClient();
        var response = await client.GetFromJsonAsync<RouteBindingResponse>("/tenants/sample/api/2147483648/items/7");
        Assert.Equal(new RouteBindingResponse(2147483648L, 7, null), response);
    }

    [Fact]
    public async Task Parent_long_token_and_route_alias_keep_values_outside_the_int_range()
    {
        await using var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        using var client = app.GetTestClient();
        var response = await client.GetFromJsonAsync<RouteBindingResponse>("/tenants/sample/api/2147483648/items/7?accountId=99&customerId=99&id=99");

        Assert.Equal(new RouteBindingResponse(2147483648L, 7, null), response);
    }

    [Fact]
    public async Task Outer_host_can_supply_the_explicit_binding_mentioned_by_the_Info_advisory()
    {
        await using var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        using var client = app.GetTestClient();
        var response = await client.GetFromJsonAsync<RouteBindingResponse>("/tenants/sample/api/42/items/host-context?tenant=ignored&tenantKey=ignored");

        Assert.Equal(new RouteBindingResponse(42, null, "sample"), response);
    }

    [Fact]
    public async Task Narrow_parameter_remains_a_framework_binding_choice_not_an_analyzer_rewrite()
    {
        await using var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        var small = await client.GetFromJsonAsync<RouteBindingResponse>("/tenants/sample/api/42/items/narrow/7");
        using var large = await client.GetAsync("/tenants/sample/api/2147483648/items/narrow/7");

        Assert.Equal(new RouteBindingResponse(42, 7, null), small);
        Assert.Equal(HttpStatusCode.BadRequest, large.StatusCode);
    }
}