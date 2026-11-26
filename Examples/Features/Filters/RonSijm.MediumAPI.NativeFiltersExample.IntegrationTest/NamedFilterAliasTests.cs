using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.NativeFiltersExample.NamedFilterRoutes;
using Xunit;
using static RonSijm.MediumAPI.NativeFiltersExample.IntegrationTest.NamedFilterAliasHost;
using ExampleProgram = RonSijm.MediumAPI.NativeFiltersExample.Program;

namespace RonSijm.MediumAPI.NativeFiltersExample.IntegrationTest;

public sealed class NamedFilterAliasTests
{
    [Fact]
    public void Named_attributes_preserve_the_fixed_generic_contract_annotation_and_native_usage()
    {
        var generic = typeof(EndpointFilterAttribute<>);
        Assert.False(generic.IsSealed);
        Assert.Equal(typeof(EndpointFilterAttribute), generic.BaseType);
        var parameter = Assert.Single(generic.GetGenericArguments());
        Assert.Equal(DynamicallyAccessedMemberTypes.PublicConstructors, parameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>()!.MemberTypes);
        foreach (var pair in new[] { (typeof(NamedGroupGuardAttribute), typeof(AliasGroupGuardFilter)), (typeof(NamedConfiguredReadAttribute), typeof(AliasConfiguredReadFilter)), (typeof(NamedArgumentAttribute), typeof(AliasArgumentFilter)) })
        {
            Assert.Equal(pair.Item2, Target(pair.Item1));
            Assert.Empty(pair.Item1.GetCustomAttributes<AttributeUsageAttribute>(inherit: false));
            var usage = pair.Item1.GetCustomAttribute<AttributeUsageAttribute>(inherit: true)!;
            Assert.Equal(AttributeTargets.Class | AttributeTargets.Method, usage.ValidOn);
            Assert.True(usage.AllowMultiple);
            Assert.True(usage.Inherited);
        }

        Assert.False(typeof(EndpointFilterFactoryAttribute<>).IsSealed);
    }

    [Fact]
    public async Task Named_generic_and_independent_native_routes_keep_full_contracts_and_API_Explorer_descriptions()
    {
        await using var native = Create("native");
        await native.StartAsync();
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            Assert.True(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()).IsEquivalent);
            Assert.True(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(app)).IsEquivalent);
            Assert.Equal(4, Routes(app).Count());
            foreach (var path in new[] { "value", "repeated", "inherited", "overridden" })
            {
                var actual = Route(app, "/named-filters/" + path + "/{value:int}");
                var expected = Route(native, actual.RoutePattern.RawText!);
                Assert.Equal(Responses(expected), Responses(actual));
                Assert.Equal(expected.Metadata.GetOrderedMetadata<NamedFilterConfiguredMetadata>().Select(metadata => metadata.Value), actual.Metadata.GetOrderedMetadata<NamedFilterConfiguredMetadata>().Select(metadata => metadata.Value));
            }

            Assert.Equal(0, Probe(app).HandlerCalls);
        }
    }

    [Theory]
    [InlineData("value", 14)]
    [InlineData("repeated", 24)]
    [InlineData("inherited", 14)]
    [InlineData("overridden", 14)]
    public async Task All_owner_placements_preserve_native_payload_headers_argument_replacement_and_result_identity(string path, int value)
    {
        await using var native = Create("native");
        await native.StartAsync();
        using var nativeClient = native.GetTestClient();
        using var expected = await nativeClient.GetAsync("/named-filters/" + path + "/4?page=7");
        Assert.Equal(HttpStatusCode.OK, expected.StatusCode);
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var actual = await client.GetAsync("/named-filters/" + path + "/4?page=7");
            await SameResponse(expected, actual);
            var reply = (await actual.Content.ReadFromJsonAsync<NamedFilterReply>())!;
            Assert.Equal(value, reply.Value);
            Assert.Equal(7, reply.Page);
            Assert.Equal(5, Probe(app).ArgumentCount);
            Assert.True(Probe(app).OriginalScopeArgument);
            Assert.Same(Probe(app).Returned, Probe(app).Observed);
            Assert.Equal(Probe(native).Constructors, Probe(app).Constructors);
            Assert.Equal(1, Probe(app).HandlerCalls);
        }
    }

    [Theory]
    [InlineData("value")]
    [InlineData("repeated")]
    public async Task The_namespace_guard_short_circuits_before_class_method_and_handler_headers(string path)
    {
        await using var native = Create("native");
        await native.StartAsync();
        using var nativeClient = native.GetTestClient();
        using var expected = await Reject(nativeClient, path);
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var actual = await Reject(client, path);
            await SameResponse(expected, actual);
            Assert.Equal(HttpStatusCode.BadRequest, actual.StatusCode);
            Assert.False(actual.Headers.Contains("X-Named-Group"));
            Assert.False(actual.Headers.Contains("X-Named-Configured"));
            Assert.False(actual.Headers.Contains("X-Named-Handler"));
            Assert.Equal(0, Probe(app).HandlerCalls);
            Assert.Equal(Probe(native).Constructors, Probe(app).Constructors);
        }
    }

    [Theory]
    [InlineData("value")]
    [InlineData("repeated")]
    public async Task Configured_filter_keeps_existing_missing_translation_and_the_same_header_boundary(string path)
    {
        await using var native = Create("native");
        await native.StartAsync();
        using var nativeClient = native.GetTestClient();
        using var expected = await nativeClient.GetAsync("/named-filters/" + path + "/4?outcome=missing");
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var actual = await client.GetAsync("/named-filters/" + path + "/4?outcome=missing");
            await SameResponse(expected, actual);
            Assert.Equal(HttpStatusCode.NotFound, actual.StatusCode);
            Assert.Equal("entry", Assert.Single(actual.Headers.GetValues("X-Named-Handler")));
            Assert.Equal("registered", Assert.Single(actual.Headers.GetValues("X-Named-Configured")));
            Assert.Equal(1, Probe(app).HandlerCalls);
        }
    }

    [Theory]
    [InlineData("/named-filters/value/not-int")]
    [InlineData("/named-filters/value/4?page=bad")]
    public async Task Native_routing_and_binding_failures_keep_status_headers_and_zero_dispatch(string path)
    {
        await using var native = Create("native");
        await native.StartAsync();
        using var nativeClient = native.GetTestClient();
        using var expected = await nativeClient.GetAsync(path);
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var actual = await client.GetAsync(path);
            await SameResponse(expected, actual);
            Assert.Equal(path.Contains("not-int", StringComparison.Ordinal) ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest, actual.StatusCode);
            Assert.Equal(0, Probe(app).HandlerCalls);
            Assert.Equal(Probe(native).Constructors, Probe(app).Constructors);
        }
    }

    [Theory]
    [InlineData("native")]
    [InlineData("generic")]
    [InlineData("named")]
    public async Task Native_filter_constructor_injection_and_handler_arguments_share_each_request_scope(string reference)
    {
        await using var app = Create(reference);
        await app.StartAsync();
        Assert.Empty(Probe(app).Constructors);
        using var client = app.GetTestClient();
        using var first = await client.GetAsync("/named-filters/value/4");
        using var second = await client.GetAsync("/named-filters/value/4");
        var firstReply = (await first.Content.ReadFromJsonAsync<NamedFilterReply>())!;
        var secondReply = (await second.Content.ReadFromJsonAsync<NamedFilterReply>())!;
        Assert.NotEqual(firstReply.ScopeId, secondReply.ScopeId);
        Assert.True(Probe(app).OriginalScopeArgument);
        Assert.Equal(new[] { "group:1", "configured:1", "argument:1", "group:2", "configured:2", "argument:2" }, Probe(app).Constructors);
        Assert.Equal(2, Probe(app).HandlerCalls);
    }

    [Fact]
    public async Task Native_method_inheritance_matches_old_broad_lookup_and_ignores_base_class_filter_declarations()
    {
        Assert.Equal(Arguments(typeof(GenericNamedOverriddenEndpoint)), Arguments(typeof(GetOverriddenNamedFilterEndpoint)));
        Assert.Equal(Arguments(typeof(GenericNamedInheritedEndpoint)), Arguments(typeof(GetInheritedNamedFilterEndpoint)));
        await using var app = Create();
        await app.StartAsync();
        Assert.Empty(Route(app, "/named-filters/inherited/{value:int}").Metadata.GetOrderedMetadata<NamedFilterConfiguredMetadata>());
        Assert.Empty(Route(app, "/named-filters/overridden/{value:int}").Metadata.GetOrderedMetadata<NamedFilterConfiguredMetadata>());
        Assert.Equal(typeof(NamedFilterMethodBase), Route(app, "/named-filters/inherited/{value:int}").Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
    }

    [Fact]
    public async Task Alias_type_decorations_are_not_imported_but_underlying_filter_metadata_and_Configure_still_apply()
    {
        await using var app = Create();
        await app.StartAsync();
        var route = Route(app, "/named-filters/value/{value:int}");
        Assert.Equal("configured", Assert.Single(route.Metadata.GetOrderedMetadata<NamedFilterConfiguredMetadata>()).Value);
        Assert.Contains(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), metadata => metadata.StatusCode == 404);
        Assert.DoesNotContain(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), metadata => metadata.StatusCode == 418);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/named-filters/value/4");
        Assert.False(response.Headers.Contains("X-Ignored-Alias"));
        Assert.Equal("registered", Assert.Single(response.Headers.GetValues("X-Named-Configured")));
    }

    [Fact]
    public async Task Explicit_aliases_keep_metadata_names_native_binding_and_fresh_host_state()
    {
        await using var native = Create("native", alias: true);
        await native.StartAsync();
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = Create(reference, alias: true);
            await app.StartAsync();
            Assert.True(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()).IsEquivalent);
            Assert.True(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(app)).IsEquivalent);
            Assert.Equal("NamedFilterAlias", Route(app, "/named-filters/alias/{value:int}").Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
            Assert.NotSame(Probe(native), Probe(app));
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/named-filters/alias/4");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(14, (await response.Content.ReadFromJsonAsync<NamedFilterReply>())!.Value);
        }
    }

    [Fact]
    public async Task Disabled_group_has_no_routes_filter_metadata_or_activation()
    {
        await using var app = Create(enabled: false);
        await app.StartAsync();
        Assert.Empty(Routes(app));
        Assert.Empty(Probe(app).Constructors);
        Assert.Equal(0, Probe(app).HandlerCalls);
    }

    [Fact]
    public async Task Public_selection_runs_named_attributes_and_default_discovery_stays_separate()
    {
        await using var selected = ExampleProgram.Build(new[] { "--Sample:NamedFilterAliases=true" }, builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        });
        await selected.StartAsync();
        Assert.Equal(4, Routes(selected).Count());
        using var client = selected.GetTestClient();
        using var response = await client.GetAsync("/named-filters/value/4");
        Assert.Equal(14, (await response.Content.ReadFromJsonAsync<NamedFilterReply>())!.Value);
        await using var original = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        });
        await original.StartAsync();
        Assert.DoesNotContain(Routes(original), route => route.RoutePattern.RawText!.StartsWith("/named-filters", StringComparison.Ordinal));
        Assert.Contains(Routes(original), route => route.RoutePattern.RawText == "/native/value/{value:int}");
    }

    private static async Task<HttpResponseMessage> Reject(HttpClient client, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/named-filters/" + path + "/4");
        request.Headers.Add("X-Reject", "true");
        return await client.SendAsync(request);
    }

    private static async Task SameResponse(HttpResponseMessage expected, HttpResponseMessage actual)
    {
        Assert.Equal(expected.StatusCode, actual.StatusCode);
        Assert.Equal(expected.Content.Headers.ContentType?.ToString(), actual.Content.Headers.ContentType?.ToString());
        Assert.Equal(await expected.Content.ReadAsByteArrayAsync(), await actual.Content.ReadAsByteArrayAsync());
        Assert.Equal(expected.Headers.Where(header => header.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase)).Select(header => header.Key + ":" + string.Join(",", header.Value)).Order(StringComparer.Ordinal), actual.Headers.Where(header => header.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase)).Select(header => header.Key + ":" + string.Join(",", header.Value)).Order(StringComparer.Ordinal));
    }

    private static string[] Responses(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(metadata => $"{metadata.GetType().FullName}|{metadata.StatusCode}|{metadata.Type?.FullName}|{string.Join(",", metadata.ContentTypes)}").ToArray();
    }
}