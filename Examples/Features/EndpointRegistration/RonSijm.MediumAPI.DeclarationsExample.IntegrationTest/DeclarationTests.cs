using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.DeclarationsExample.Program;

namespace RonSijm.MediumAPI.DeclarationsExample.IntegrationTest;

// Future regression sources only. No examples, hosts or tests ran for this addition.
public sealed class DeclarationTests
{
    [Theory]
    [InlineData("/api/internal", "internal")]
    [InlineData("/api/partial", "partial")]
    [InlineData("/api/inherited", "inherited")]
    [InlineData("/api/optional/generic", "generic")]
    public async Task Supported_declarations_retain_their_typed_http_contract(string path, string shape)
    {
        await using var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(new DeclarationResponse(shape), await response.Content.ReadFromJsonAsync<DeclarationResponse>());
    }

    [Fact]
    public async Task Partial_endpoint_and_group_parts_register_one_route_with_the_declared_convention()
    {
        await using var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        var source = app.Services.GetRequiredService<EndpointDataSource>();
        var routes = source.Endpoints.OfType<RouteEndpoint>().ToArray();
        var partial = Assert.Single(routes, route => route.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "PartialDeclaration");

        Assert.Equal(4, routes.Length);
        Assert.Equal("/api/partial", partial.RoutePattern.RawText);
        Assert.Equal("One endpoint declared across source files", partial.Metadata.GetMetadata<IEndpointSummaryMetadata>()?.Summary);
        Assert.Contains(partial.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 200 && response.Type == typeof(DeclarationResponse));
        Assert.False(app.GetMediumApiRouteReport().HasConflicts);
    }

    [Fact]
    public async Task Disabled_child_omits_only_its_namespace_endpoint()
    {
        await using var app = ExampleProgram.Build(options: new DeclarationOptions(EnableOptional: false), configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var ordinary = await client.GetAsync("/api/partial");
        using var optional = await client.GetAsync("/api/optional/generic");

        Assert.Equal(HttpStatusCode.OK, ordinary.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, optional.StatusCode);
    }

    [Fact]
    public async Task Disabled_partial_parent_has_no_root_fallback_for_its_descendants()
    {
        await using var app = ExampleProgram.Build(options: new DeclarationOptions(EnableApi: false), configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/api/internal", "/api/partial", "/api/inherited", "/api/optional/generic", "/internal", "/partial", "/inherited", "/generic" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}