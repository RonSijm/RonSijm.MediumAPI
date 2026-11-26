using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.HostRoutingExample.Program;

namespace RonSijm.MediumAPI.HostRoutingExample.IntegrationTest;

internal static class HostRoutingHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true)
    {
        var app = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer(), mapEndpoints: mapEndpoints && !native);
        if (native && mapEndpoints)
        {
            MapNativeReference(app);
        }

        return app;
    }

    internal static HostProbe Probe(WebApplication app) => app.Services.GetRequiredService<HostProbe>();
    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    internal static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    internal static string[] MetadataInventory(WebApplication app) => Routes(app).Select(endpoint => string.Join("|", endpoint.RoutePattern.RawText, endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, string.Join(";", endpoint.Metadata.GetOrderedMetadata<IHostMetadata>().Select(metadata => string.Join(",", metadata.Hosts))))).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    internal static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    internal static Ok<HostResponse> Reply(HostProbe probe, string value) => TypedResults.Ok(probe.Record(value));

    internal static async Task<HttpResponseMessage> Send(HttpClient client, string path, string? host, string scheme = "http")
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(scheme + "://transport.invalid" + path));
        if (host is not null)
        {
            request.Headers.Host = host;
        }

        return await client.SendAsync(request);
    }

    internal static async Task<HostResponse> Read(HttpClient client, string path, string? host, string scheme = "http")
    {
        using var response = await Send(client, path, host, scheme);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<HostResponse>())!;
    }

    private static RouteGroupBuilder ClassGroup(IEndpointRouteBuilder parent, params string[] hosts)
    {
        var group = parent.MapGroup("");
        group.RequireHost(hosts);
        return group;
    }

    private static void MapNativeReference(WebApplication app)
    {
        var group = app.MapGroup("/hosts");
        group.RequireHost(SampleHosts.Group);
        group.MapGet("/inherited", NativeInherited).WithName("GetInheritedHost");
        ClassGroup(group, SampleHosts.Api, SampleHosts.Alternate).MapGet("/class", NativeClass).WithName("GetClassHosts");
        ClassGroup(group, SampleHosts.Api).MapGet("/method", NativeMethod).WithName("GetMethodHost");
        ClassGroup(group, SampleHosts.Api).MapGet("/configured", NativeConfigured).WithName("GetConfiguredHost").RequireHost(SampleHosts.Configured);
        ClassGroup(group, SampleHosts.Wildcard).MapGet("/wildcard", NativeWildcard).WithName("GetWildcardHost");
        ClassGroup(group, SampleHosts.Port, SampleHosts.SecurePort, SampleHosts.HttpPort).MapGet("/port", NativePort).WithName("GetPortHost");
        ClassGroup(group).MapGet("/empty", NativeEmpty).WithName("GetEmptyHost");
        var child = group.MapGroup("/child");
        child.RequireHost(SampleHosts.Child);
        child.MapGet("/value", NativeChild).WithName("GetChildHost");
        app.MapGet("/open", NativeOpen).WithName("GetOpenHost");
    }

    private static Ok<HostResponse> NativeInherited(HostProbe probe) => Reply(probe, "inherited");
    private static Ok<HostResponse> NativeClass(HostProbe probe) => Reply(probe, "class");
    private static Ok<HostResponse> NativeWildcard(HostProbe probe) => Reply(probe, "wildcard");
    private static Ok<HostResponse> NativePort(HostProbe probe) => Reply(probe, "port");
    private static Ok<HostResponse> NativeEmpty(HostProbe probe) => Reply(probe, "empty");
    private static Ok<HostResponse> NativeChild(HostProbe probe) => Reply(probe, "child");
    private static Ok<HostResponse> NativeOpen(HostProbe probe) => Reply(probe, "open");

    [Host(SampleHosts.Method)]
    private static Ok<HostResponse> NativeMethod(HostProbe probe) => Reply(probe, "method");

    [Host(SampleHosts.Method)]
    private static Ok<HostResponse> NativeConfigured(HostProbe probe) => Reply(probe, "configured");
}