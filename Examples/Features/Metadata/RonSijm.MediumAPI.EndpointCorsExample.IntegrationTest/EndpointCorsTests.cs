using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest.EndpointCorsHost;

namespace RonSijm.MediumAPI.EndpointCorsExample.IntegrationTest;

public sealed class EndpointCorsTests
{
    [Fact]
    public async Task Class_CORS_matches_independent_native_route_API_Explorer_and_ordered_CORS_inventories()
    {
        await using var native = Create(native: true);
        await using var generated = Create();
        await native.StartAsync();
        await generated.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.Equal(CorsInventory(native), CorsInventory(generated));
        Assert.Equal(8, Routes(generated).Count());
        Assert.Equal(8, Descriptions(generated).Operations.Count);
        Assert.All(Routes(generated), endpoint => Assert.Equal(new[] { "GET" }, endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods));
        Assert.Equal(0, Probe(native).TotalCalls);
        Assert.Equal(0, Probe(generated).TotalCalls);
    }

    [Theory]
    [InlineData("/cors/default", false, null)]
    [InlineData("/cors/named", false, SamplePolicies.Browser)]
    [InlineData("/cors/enabled-group/class-disabled", true, null)]
    [InlineData("/cors/disabled-group/class-enabled", false, SamplePolicies.Browser)]
    [InlineData("/cors/class-disabled-method-enabled", false, SamplePolicies.Browser)]
    [InlineData("/cors/class-enabled-method-disabled", true, null)]
    [InlineData("/cors/configured", false, SamplePolicies.Override)]
    public async Task Effective_CORS_uses_native_last_metadata_precedence_and_enables_native_preflight_matching(string path, bool disabled, string? policyName)
    {
        await using var app = Create();
        await app.StartAsync();
        var endpoint = Route(app, path);
        var metadata = endpoint.Metadata.GetMetadata<ICorsMetadata>();
        if (disabled)
        {
            Assert.IsType<DisableCorsAttribute>(metadata);
        }
        else
        {
            Assert.Equal(policyName, Assert.IsType<EnableCorsAttribute>(metadata).PolicyName);
        }

        Assert.True(endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.AcceptCorsPreflight);
    }

    [Theory]
    [InlineData("/cors/default", "default", SamplePolicies.DefaultOrigin, true)]
    [InlineData("/cors/named", "named", SamplePolicies.BrowserOrigin, true)]
    [InlineData("/cors/named", "named", SamplePolicies.OtherBrowserOrigin, true)]
    [InlineData("/cors/named", "named", SamplePolicies.RejectedOrigin, false)]
    [InlineData("/cors/enabled-group/class-disabled", "class-disabled", SamplePolicies.BrowserOrigin, false)]
    [InlineData("/cors/disabled-group/class-enabled", "class-enabled", SamplePolicies.BrowserOrigin, true)]
    [InlineData("/cors/class-disabled-method-enabled", "method-enabled", SamplePolicies.BrowserOrigin, true)]
    [InlineData("/cors/class-enabled-method-disabled", "method-disabled", SamplePolicies.BrowserOrigin, false)]
    [InlineData("/cors/configured", "configured", SamplePolicies.OverrideOrigin, true)]
    [InlineData("/cors/configured", "configured", SamplePolicies.BrowserOrigin, false)]
    [InlineData("/cors/open", "open", SamplePolicies.DefaultOrigin, true)]
    public async Task Native_CORS_controls_browser_allow_headers_while_ordinary_GET_requests_still_execute(string path, string handler, string origin, bool allowed)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, path, origin);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new CorsReceipt(handler), await response.Content.ReadFromJsonAsync<CorsReceipt>());
            Assert.Equal(allowed ? new[] { origin } : [], HeaderValues(response, "Access-Control-Allow-Origin"));
            Assert.Equal(1, Probe(app).TotalCalls);
            Assert.Equal(1, Probe(app).CallsFor(handler));
        }
    }

    [Theory]
    [InlineData("/cors/default", "default")]
    [InlineData("/cors/named", "named")]
    [InlineData("/cors/enabled-group/class-disabled", "class-disabled")]
    public async Task Requests_without_Origin_keep_normal_handler_behavior_without_CORS_response_headers(string path, string handler)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new CorsReceipt(handler), await response.Content.ReadFromJsonAsync<CorsReceipt>());
            Assert.Empty(HeaderValues(response, "Access-Control-Allow-Origin"));
            Assert.Empty(HeaderValues(response, "Access-Control-Allow-Credentials"));
            Assert.Empty(HeaderValues(response, "Access-Control-Expose-Headers"));
            Assert.Equal(1, Probe(app).CallsFor(handler));
        }
    }

    [Theory]
    [InlineData("/cors/default", SamplePolicies.DefaultOrigin, true)]
    [InlineData("/cors/named", SamplePolicies.BrowserOrigin, true)]
    [InlineData("/cors/named", SamplePolicies.RejectedOrigin, false)]
    [InlineData("/cors/enabled-group/class-disabled", SamplePolicies.BrowserOrigin, false)]
    [InlineData("/cors/disabled-group/class-enabled", SamplePolicies.BrowserOrigin, true)]
    [InlineData("/cors/class-disabled-method-enabled", SamplePolicies.BrowserOrigin, true)]
    [InlineData("/cors/class-enabled-method-disabled", SamplePolicies.BrowserOrigin, false)]
    [InlineData("/cors/configured", SamplePolicies.OverrideOrigin, true)]
    public async Task Native_preflight_returns_204_before_handler_execution_with_only_the_effective_policy_headers(string path, string origin, bool allowed)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Preflight(client, path, origin, requestedHeaders: path == "/cors/default" ? SamplePolicies.DefaultRequestHeader : SamplePolicies.RequestHeader);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(allowed ? new[] { origin } : [], HeaderValues(response, "Access-Control-Allow-Origin"));
            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }

    [Fact]
    public async Task Named_policy_preserves_native_credentials_exposed_headers_methods_headers_and_max_age()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var get = await Send(client, "/cors/named", SamplePolicies.BrowserOrigin);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.Equal(new[] { "true" }, HeaderValues(get, "Access-Control-Allow-Credentials"));
            Assert.Equal(new[] { SamplePolicies.ResponseHeader.ToLowerInvariant() }, HeaderValues(get, "Access-Control-Expose-Headers").Select(value => value.ToLowerInvariant()));
            Assert.Contains(HeaderValues(get, "Vary"), value => string.Equals(value, "Origin", StringComparison.OrdinalIgnoreCase));
            using var preflight = await Preflight(client, "/cors/named", SamplePolicies.BrowserOrigin, requestedHeaders: SamplePolicies.RequestHeader);
            Assert.Equal(HttpStatusCode.NoContent, preflight.StatusCode);
            Assert.Equal(new[] { "true" }, HeaderValues(preflight, "Access-Control-Allow-Credentials"));
            Assert.Equal(new[] { "GET", "POST" }, HeaderValues(preflight, "Access-Control-Allow-Methods"));
            Assert.Equal(new[] { SamplePolicies.RequestHeader.ToLowerInvariant() }, HeaderValues(preflight, "Access-Control-Allow-Headers").Select(value => value.ToLowerInvariant()));
            Assert.Equal(new[] { "600" }, HeaderValues(preflight, "Access-Control-Max-Age"));
            // The server advertises its policy. A browser compares its requested header to that list.
            using var unsupportedHeader = await Preflight(client, "/cors/named", SamplePolicies.BrowserOrigin, requestedHeaders: "X-Not-Allowed");
            Assert.Equal(HttpStatusCode.NoContent, unsupportedHeader.StatusCode);
            Assert.Equal(new[] { SamplePolicies.RequestHeader.ToLowerInvariant() }, HeaderValues(unsupportedHeader, "Access-Control-Allow-Headers").Select(value => value.ToLowerInvariant()));
            Assert.Equal(1, Probe(app).TotalCalls);
        }
    }

    [Fact]
    public async Task Bare_EnableCors_retains_the_named_middleware_policy_and_explicit_class_names_override_it()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native, middlewarePolicyName: SamplePolicies.Override);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var selected = await Send(client, "/cors/default", SamplePolicies.OverrideOrigin);
            Assert.Equal(new[] { SamplePolicies.OverrideOrigin }, HeaderValues(selected, "Access-Control-Allow-Origin"));
            using var notDefault = await Send(client, "/cors/default", SamplePolicies.DefaultOrigin);
            Assert.Empty(HeaderValues(notDefault, "Access-Control-Allow-Origin"));
            using var named = await Send(client, "/cors/named", SamplePolicies.BrowserOrigin);
            Assert.Equal(new[] { SamplePolicies.BrowserOrigin }, HeaderValues(named, "Access-Control-Allow-Origin"));
            Assert.Null(Assert.IsType<EnableCorsAttribute>(Route(app, "/cors/default").Metadata.GetMetadata<ICorsMetadata>()).PolicyName);
            Assert.Equal(3, Probe(app).TotalCalls);
        }
    }

    [Fact]
    public async Task Independent_hosts_keep_native_named_policy_configuration_and_handler_probes_isolated()
    {
        foreach (var native in new[] { true, false })
        {
            const string firstOrigin = "https://first-host.example";
            const string secondOrigin = "https://second-host.example";
            await using var first = Create(native, configure: builder => builder.Services.Configure<CorsOptions>(options => options.AddPolicy(SamplePolicies.Browser, policy => policy.WithOrigins(firstOrigin).AllowAnyHeader().AllowAnyMethod())));
            await using var second = Create(native, configure: builder => builder.Services.Configure<CorsOptions>(options => options.AddPolicy(SamplePolicies.Browser, policy => policy.WithOrigins(secondOrigin).AllowAnyHeader().AllowAnyMethod())));
            await first.StartAsync();
            await second.StartAsync();
            using var firstClient = first.GetTestClient();
            using var secondClient = second.GetTestClient();
            using var firstAllowed = await Send(firstClient, "/cors/named", firstOrigin);
            using var secondRejected = await Send(secondClient, "/cors/named", firstOrigin);
            using var secondAllowed = await Send(secondClient, "/cors/named", secondOrigin);
            Assert.Equal(new[] { firstOrigin }, HeaderValues(firstAllowed, "Access-Control-Allow-Origin"));
            Assert.Empty(HeaderValues(secondRejected, "Access-Control-Allow-Origin"));
            Assert.Equal(new[] { secondOrigin }, HeaderValues(secondAllowed, "Access-Control-Allow-Origin"));
            Assert.Equal(1, Probe(first).TotalCalls);
            Assert.Equal(2, Probe(second).TotalCalls);
        }
    }

    [Fact]
    public async Task Native_names_links_original_method_signature_and_typed_response_metadata_are_preserved()
    {
        await using var app = Create();
        await app.StartAsync();
        var links = app.Services.GetRequiredService<LinkGenerator>();
        foreach (var endpoint in Routes(app))
        {
            var name = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName;
            Assert.Equal(endpoint.RoutePattern.RawText, links.GetPathByName(name, values: null));
            var method = endpoint.Metadata.GetMetadata<MethodInfo>()!;
            Assert.Equal("HandleAsync", method.Name);
            Assert.Equal(typeof(CorsProbe), Assert.Single(method.GetParameters()).ParameterType);
            Assert.Equal(typeof(Ok<CorsReceipt>), method.ReturnType);
        }

        Assert.Equal(0, Probe(app).TotalCalls);
    }
}