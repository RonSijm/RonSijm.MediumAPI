using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ContractProgram = RonSijm.MediumAPI.ContractsExample.Program;

namespace RonSijm.MediumAPI.ContractsExample.IntegrationTest;

// Exercises explicit snapshot capture and structural comparison with synthetic contract data.
public sealed class ContractExamplesTests
{
    [Fact]
    public async Task Constant_routes_retain_binding_names_and_their_composed_paths()
    {
        await using var app = ContractProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/items")).StatusCode);
        using var response = await client.GetAsync("/api/items/42");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(42, body.RootElement.GetProperty("id").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/items/0")).StatusCode);
        Assert.Equal("/api/items/42", app.Services.GetRequiredService<LinkGenerator>().GetPathByName("ContractsGetItem", new { id = 42 }));
        var snapshot = app.GetMediumApiContractSnapshot();
        Assert.Contains(snapshot.Routes, route => route.Pattern == "/api/items/{id:int}" && route.Name == "ContractsGetItem");
        Assert.Contains(snapshot.Routes, route => route.Pattern == "/api/Status");
    }

    [Fact]
    public async Task Builder_and_composite_source_capture_the_same_host_contract()
    {
        await using var app = ContractProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        var builderSnapshot = app.GetMediumApiContractSnapshot();
        var sourceSnapshot = app.Services.GetRequiredService<EndpointDataSource>().GetMediumApiContractSnapshot();
        var comparison = MediumApiContractComparison.Compare(builderSnapshot, sourceSnapshot);
        Assert.True(comparison.IsEquivalent);
        Assert.Empty(comparison.Differences);
    }

    [Fact]
    public async Task Snapshot_comparison_reports_an_added_framework_route()
    {
        await using var baseline = ContractProgram.Build();
        await using var changed = ContractProgram.Build();
        changed.MapGet("/extra", () => Results.Ok()).WithName("ExtraRoute");
        var comparison = MediumApiContractComparison.Compare(baseline.GetMediumApiContractSnapshot(), changed.GetMediumApiContractSnapshot());
        Assert.False(comparison.IsEquivalent);
        Assert.Contains(comparison.Differences, difference => difference.Path.StartsWith("/routes/", StringComparison.Ordinal));
        Assert.Throws<InvalidOperationException>(comparison.ThrowIfDifferent);
    }

    [Fact]
    public void Json_object_order_is_ignored_but_operation_tag_order_is_retained()
    {
        const string expected = """{"openapi":"3.0.0","paths":{"/items":{"get":{"tags":["a","b"]}}}}""";
        const string reorderedObjects = """{"paths":{"/items":{"get":{"tags":["a","b"]}}},"openapi":"3.0.0"}""";
        const string reorderedTags = """{"openapi":"3.0.0","paths":{"/items":{"get":{"tags":["b","a"]}}}}""";
        Assert.True(MediumApiContractComparison.CompareOpenApi(expected, reorderedObjects).IsEquivalent);
        var comparison = MediumApiContractComparison.CompareOpenApi(expected, reorderedTags);
        Assert.False(comparison.IsEquivalent);
        Assert.Contains(comparison.Differences, difference => difference.Path == "/paths/~1items/get/tags/0");
    }

    [Fact]
    public void Ambiguous_or_invalid_json_is_rejected_instead_of_normalized()
    {
        Assert.Throws<ArgumentException>(() => MediumApiContractComparison.CompareJson("""{"name":1,"name":2}""", "{}"));
        // System.Text.Json may throw a more specific JsonException implementation.
        Assert.ThrowsAny<JsonException>(() => MediumApiContractComparison.CompareOpenApi("{", "{}"));
        Assert.ThrowsAny<JsonException>(() => MediumApiContractComparison.CompareOpenApi("{}", "{"));
        Assert.ThrowsAny<JsonException>(() => MediumApiContractComparison.CompareJson("{", "{}"));
        Assert.ThrowsAny<JsonException>(() => MediumApiContractComparison.CompareJson("{}", "{"));
        Assert.Throws<ArgumentException>(() => MediumApiContractComparison.CompareOpenApi("[]", "{}"));
    }
}