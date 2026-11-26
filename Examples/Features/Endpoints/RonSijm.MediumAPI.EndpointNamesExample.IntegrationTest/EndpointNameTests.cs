using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.EndpointNamesExample.Endpoints.Api.Disabled;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.EndpointNamesExample.Program;

namespace RonSijm.MediumAPI.EndpointNamesExample.IntegrationTest;

public sealed class EndpointNameTests
{
    [Theory]
    [InlineData("/api/products/{id:int}", "GetProduct")]
    [InlineData("/api/catalog-view", "CatalogView")]
    [InlineData("/api/diagnostics", "EndpointDiagnostics")]
    [InlineData("/api/repeated-suffix", "GetEndpoint")]
    [InlineData("/api/case-sensitive", "Lowercaseendpoint")]
    [InlineData("/api/exact-endpoint", "Endpoint")]
    public async Task Default_name_removes_only_one_exact_terminal_suffix_and_populates_both_metadata_contracts(string pattern, string expected)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        AssertEffectiveName(Endpoint(app, pattern), expected);
    }

    [Theory]
    [InlineData("/api/attribute", "NamedByAttribute")]
    [InlineData("/api/fluent", "NamedByFluent")]
    [InlineData("/api/filter", "NamedByFilter")]
    [InlineData("/api/group-named/", "NamedByGroup")]
    [InlineData("/aliases/products/{id:int}", "ProductAlias")]
    [InlineData("/aliases/attribute-override", "HostOverride")]
    [InlineData("/aliases/explicit-suppressed/{id:int}", "ExplicitSuppressedAlias")]
    [InlineData("/api/unnamed/explicit", "ExplicitInsideUnnamedGroup")]
    public async Task Explicit_names_survive_the_fallback_finalizer_and_suppression(string pattern, string expected)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        AssertEffectiveName(Endpoint(app, pattern), expected);
    }

    [Theory]
    [InlineData("/api/group-named")]
    [InlineData("/api/group-named/")]
    public async Task Empty_relative_pattern_retains_native_group_route_matching(string path)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("group", await response.Content.ReadFromJsonAsync<string>());
        // Native composition retains this separator in RawText for the empty leaf.
        AssertEffectiveName(Endpoint(app, "/api/group-named/"), "NamedByGroup");
    }

    [Fact]
    public async Task Default_and_explicit_alias_names_generate_their_own_paths()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        var links = app.Services.GetRequiredService<LinkGenerator>();

        Assert.Equal("/api/products/7", links.GetPathByName("GetProduct", new { id = 7 }));
        Assert.Equal("/aliases/products/7", links.GetPathByName("ProductAlias", new { id = 7 }));
        Assert.Equal("/aliases/explicit-suppressed/7", links.GetPathByName("ExplicitSuppressedAlias", new { id = 7 }));
        Assert.Equal("/aliases/attribute-override", links.GetPathByName("HostOverride", values: null));
    }

    [Fact]
    public async Task Unnamed_aliases_remain_reachable_without_duplicate_inferred_names()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var prefix in new[] { "/aliases/unnamed", "/aliases/also-unnamed" })
        {
            var endpoint = Endpoint(app, prefix + "/{id:int}");
            Assert.Null(endpoint.Metadata.GetMetadata<IEndpointNameMetadata>());
            Assert.Null(endpoint.Metadata.GetMetadata<IRouteNameMetadata>());
            Assert.Equal(new ProductResponse(7, "Sample notebook"), await client.GetFromJsonAsync<ProductResponse>(prefix + "/7"));
        }

        var names = app.Services.GetRequiredService<EndpointDataSource>().Endpoints.Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName).OfType<string>().ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("/api/products/7", app.Services.GetRequiredService<LinkGenerator>().GetPathByName("GetProduct", new { id = 7 }));
    }

    [Fact]
    public async Task Group_suppression_removes_only_the_inferred_member_name()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        var unnamed = Endpoint(app, "/api/unnamed/status");

        Assert.Null(unnamed.Metadata.GetMetadata<IEndpointNameMetadata>());
        Assert.Null(unnamed.Metadata.GetMetadata<IRouteNameMetadata>());
        AssertEffectiveName(Endpoint(app, "/api/unnamed/explicit"), "ExplicitInsideUnnamedGroup");
    }

    [Fact]
    public async Task Disabled_namespaces_and_typed_mounts_do_not_publish_a_default_name()
    {
        await using var app = CreateHost();
        Assert.Null(app.TryMapMediumApiNamespace<DisabledNamesGroupDefinition>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/disabled");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(app.Services.GetRequiredService<LinkGenerator>().GetPathByName("Hidden", values: null));
    }

    [Fact]
    public async Task Fresh_hosts_keep_independent_endpoint_instances_and_the_same_default_contract()
    {
        await using var first = CreateHost();
        await using var second = CreateHost();
        await first.StartAsync();
        await second.StartAsync();
        var firstEndpoint = Endpoint(first, "/api/products/{id:int}");
        var secondEndpoint = Endpoint(second, "/api/products/{id:int}");

        Assert.NotSame(firstEndpoint, secondEndpoint);
        AssertEffectiveName(firstEndpoint, "GetProduct");
        AssertEffectiveName(secondEndpoint, "GetProduct");
        Assert.Equal("/api/products/5", first.Services.GetRequiredService<LinkGenerator>().GetPathByName("GetProduct", new { id = 5 }));
        Assert.Equal("/api/products/5", second.Services.GetRequiredService<LinkGenerator>().GetPathByName("GetProduct", new { id = 5 }));
    }

    private static void AssertEffectiveName(RouteEndpoint endpoint, string expected)
    {
        Assert.Equal(expected, endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
        Assert.Equal(expected, endpoint.Metadata.GetMetadata<IRouteNameMetadata>()?.RouteName);
    }

    private static WebApplication CreateHost() => ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());

    private static RouteEndpoint Endpoint(WebApplication app, string pattern) => Assert.Single(app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>(), endpoint => endpoint.RoutePattern.RawText == pattern);
}