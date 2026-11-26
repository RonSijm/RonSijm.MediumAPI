using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.Example.Endpoints;
using Xunit;

namespace RonSijm.MediumAPI.Example.IntegrationTest;

public sealed class EndpointGroupTests
{
    [Fact]
    public async Task Configuration_runs_without_creating_scoped_dependencies_and_activation_is_per_request()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var probe = factory.Services.GetRequiredService<EndpointActivationProbe>();
        Assert.Equal(0, probe.Count);
        var first = await client.GetFromJsonAsync<Guid>("/first/value");
        var second = await client.GetFromJsonAsync<Guid>("/first/value");
        Assert.NotEqual(first, second);
        Assert.Equal(2, probe.Count);
    }

    [Theory]
    [InlineData("/first/value", "first")]
    [InlineData("/second/value", "second")]
    public async Task Groups_share_relative_patterns_without_leaking_routes_or_filters(string path, string group)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(group, response.Headers.GetValues("X-Group").Single());
        if (group == "first")
        {
            Assert.Equal("first", response.Headers.GetValues("X-Endpoint").Single());
        }
        else
        {
            Assert.False(response.Headers.Contains("X-Endpoint"));
        }

        using var missing = await client.GetAsync("/value");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var links = factory.Services.GetRequiredService<LinkGenerator>();
        Assert.Equal("/first/value", links.GetPathByName("GetFirstValue", new { }));
        Assert.Equal("/second/value", links.GetPathByName("GetSecondValue", new { }));
    }

    [Fact]
    public async Task Individual_registration_supports_aliases_and_startup_conventions()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        Assert.Equal("manual", await client.GetFromJsonAsync<string>("/alias"));
        using var missing = await client.GetAsync("/manual");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("/alias", factory.Services.GetRequiredService<LinkGenerator>().GetPathByName("ManualAlias", new { }));
    }
}