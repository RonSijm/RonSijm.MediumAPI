using System.Net;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using static RonSijm.MediumAPI.HostRoutingExample.IntegrationTest.HostRoutingHost;

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest;

public sealed class NativeHostRoutingTests
{
    [Fact]
    public async Task Class_and_namespace_host_declarations_match_independent_native_routes_and_descriptions()
    {
        await using var generated = Create();
        await using var native = Create(native: true);
        await generated.StartAsync();
        await native.StartAsync();
        Assert.Equal(9, Routes(generated).Count());
        Assert.Equal(MetadataInventory(native), MetadataInventory(generated));
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.Equal(0, Probe(generated).Calls);
        Assert.Equal(0, Probe(native).Calls);
    }

    [Theory]
    [InlineData("/hosts/inherited", SampleHosts.Group)]
    [InlineData("/hosts/class", SampleHosts.Api + "," + SampleHosts.Alternate)]
    [InlineData("/hosts/method", SampleHosts.Method)]
    [InlineData("/hosts/configured", SampleHosts.Configured)]
    [InlineData("/hosts/wildcard", SampleHosts.Wildcard)]
    [InlineData("/hosts/port", SampleHosts.Port + "," + SampleHosts.SecurePort + "," + SampleHosts.HttpPort)]
    [InlineData("/hosts/empty", "")]
    [InlineData("/hosts/child/value", SampleHosts.Child)]
    [InlineData("/open", null)]
    public async Task Effective_host_lists_preserve_native_replacement_and_absent_vs_empty_metadata(string path, string? expected)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var metadata = Route(app, path).Metadata.GetMetadata<IHostMetadata>();
            if (expected is null)
            {
                Assert.Null(metadata);
            }
            else
            {
                Assert.Equal(expected, string.Join(",", metadata!.Hosts));
            }
        }
    }

    [Theory]
    [InlineData("/hosts/inherited", "inherited", SampleHosts.Group, SampleHosts.Api)]
    [InlineData("/hosts/class", "class", SampleHosts.Api, SampleHosts.Group)]
    [InlineData("/hosts/class", "class", SampleHosts.Alternate, SampleHosts.Group)]
    [InlineData("/hosts/method", "method", SampleHosts.Method, SampleHosts.Api)]
    [InlineData("/hosts/configured", "configured", SampleHosts.Configured, SampleHosts.Method)]
    [InlineData("/hosts/child/value", "child", SampleHosts.Child, SampleHosts.Group)]
    public async Task Group_class_method_and_fluent_host_selection_matches_native_HTTP_and_rejects_the_replaced_host(string path, string value, string accepted, string rejected)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var wrong = await Send(client, path, rejected);
            Assert.Equal(HttpStatusCode.NotFound, wrong.StatusCode);
            Assert.Equal(0, Probe(app).Calls);
            Assert.Equal(new HostResponse(value), await Read(client, path, accepted));
            Assert.Equal(1, Probe(app).CallsFor(value));
            Assert.Equal(1, Probe(app).Calls);
        }
    }

    [Theory]
    [InlineData("leaf.example.test", true)]
    [InlineData("nested.leaf.example.test", true)]
    [InlineData("example.test", false)]
    [InlineData("example.test.attacker.test", false)]
    [InlineData("other.test", false)]
    public async Task Native_suffix_wildcards_accept_subdomains_and_keep_the_apex_and_other_suffixes_out(string host, bool allowed)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, "/hosts/wildcard", host);
            Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(allowed ? 1 : 0, Probe(app).CallsFor("wildcard"));
        }
    }

    [Theory]
    [InlineData("port.example.test:8443", "http", true)]
    [InlineData("port.example.test", "http", false)]
    [InlineData("port.example.test:8444", "http", false)]
    [InlineData("secure.example.test", "https", true)]
    [InlineData("secure.example.test", "http", false)]
    [InlineData("web.example.test", "http", true)]
    [InlineData("web.example.test", "https", false)]
    public async Task Explicit_ports_and_native_HTTP_HTTPS_default_ports_are_preserved(string host, string scheme, bool allowed)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, "/hosts/port", host, scheme);
            Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(allowed ? 1 : 0, Probe(app).CallsFor("port"));
        }
    }

    [Fact]
    public async Task Native_host_comparison_is_case_insensitive_and_an_omitted_pattern_port_accepts_any_port()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new HostResponse("inherited"), await Read(client, "/hosts/inherited", "GROUP.EXAMPLE.TEST:5017"));
            Assert.Equal(new HostResponse("class"), await Read(client, "/hosts/class", "API.EXAMPLE.TEST:9443", "https"));
            Assert.Equal(2, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Absent_and_explicitly_empty_host_metadata_accept_arbitrary_hosts_without_intersecting_the_parent()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var host in new string?[] { null, "arbitrary.other.test:9191" })
            {
                Assert.Equal(new HostResponse("open"), await Read(client, "/open", host));
                Assert.Equal(new HostResponse("empty"), await Read(client, "/hosts/empty", host));
            }

            Assert.Equal(4, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Ordered_native_metadata_retains_namespace_class_original_method_and_Configure_values()
    {
        await using var app = Create();
        Assert.Equal(new[] { SampleHosts.Group, SampleHosts.Api, SampleHosts.Method }, Route(app, "/hosts/method").Metadata.GetOrderedMetadata<IHostMetadata>().Select(metadata => string.Join(",", metadata.Hosts)));
        Assert.Equal(new[] { SampleHosts.Group, SampleHosts.Api, SampleHosts.Method, SampleHosts.Configured }, Route(app, "/hosts/configured").Metadata.GetOrderedMetadata<IHostMetadata>().Select(metadata => string.Join(",", metadata.Hosts)));
        Assert.Equal(new[] { SampleHosts.Group, SampleHosts.Child }, Route(app, "/hosts/child/value").Metadata.GetOrderedMetadata<IHostMetadata>().Select(metadata => string.Join(",", metadata.Hosts)));
    }
}