using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.ResponseHeadersExample.SetPolicies;
using RonSijm.MediumAPI.ResponseHeadersExample.SetPolicies.Disabled;
using Xunit;
using static RonSijm.MediumAPI.ResponseHeadersExample.IntegrationTest.HeaderSetTestHost;

namespace RonSijm.MediumAPI.ResponseHeadersExample.IntegrationTest;

public sealed class HeaderSetTests
{
    [Fact]
    public void Actual_runtime_set_copies_only_membership_and_retains_order_duplicates_subtypes_and_mutable_children()
    {
        var first = new SampleNoStoreAttribute();
        var second = new SampleNoCachePragmaAttribute();
        var input = new List<ResponseHeaderAttribute> { first, second, first };
        var set = new HeaderListAttribute(input);
        input.Clear();
        Assert.Equal(3, set.Headers.Count);
        Assert.Same(first, set.Headers[0]);
        Assert.Same(second, set.Headers[1]);
        Assert.Same(first, set.Headers[2]);
        Assert.Throws<NotSupportedException>(() => ((IList<ResponseHeaderAttribute>)set.Headers)[0] = second);
        first.ResultType = typeof(Ok<>);
        Assert.Equal(typeof(Ok<>), set.Headers[0].ResultType);
        Assert.Equal(typeof(Ok<>), set.Headers[2].ResultType);
        Assert.Empty(new HeaderListAttribute(Array.Empty<ResponseHeaderAttribute>()).Headers);
        Assert.Equal("headers", Assert.Throws<ArgumentNullException>(() => new HeaderListAttribute(null!)).ParamName);
        Assert.Equal("headers", Assert.Throws<ArgumentException>(() => new HeaderListAttribute(new ResponseHeaderAttribute[] { null! })).ParamName);
        Assert.Equal(typeof(ResponseHeaderDeclarationAttribute), typeof(ResponseHeaderAttribute).BaseType);
        Assert.Equal(typeof(ResponseHeaderDeclarationAttribute), typeof(ResponseHeadersAttribute).BaseType);
        Assert.True(typeof(ResponseHeaderDeclarationAttribute).IsAbstract);
        Assert.True(Assert.Single(typeof(ResponseHeaderDeclarationAttribute).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)).IsAssembly);
        var usage = Assert.Single(typeof(ResponseHeadersAttribute).GetCustomAttributes<AttributeUsageAttribute>());
        Assert.Equal(AttributeTargets.Class | AttributeTargets.Method, usage.ValidOn);
        Assert.True(usage.AllowMultiple);
        Assert.True(usage.Inherited);
        Assert.Null(typeof(ResponseHeadersAttribute).GetProperty(nameof(ResponseHeadersAttribute.Headers))!.SetMethod);
        Assert.False(typeof(ResponseHeaderAttribute).IsAssignableFrom(typeof(HeaderListAttribute)));
        var cache = new SampleNoCacheHeadersAttribute();
        Assert.IsType<SampleNoStoreAttribute>(cache.Headers[0]);
        Assert.IsType<SampleNoCachePragmaAttribute>(cache.Headers[1]);
        Assert.Equal(new[] { "Cache-Control", "Pragma" }, cache.Headers.Select(header => header.Name));
    }

    [Fact]
    public async Task Complete_route_and_API_Explorer_contracts_match_old_singles_and_independent_native_mappings()
    {
        await using var app = Create();
        await app.StartAsync();
        foreach (var reference in new[] { "old", "native" })
        {
            await using var expected = Create(reference);
            await expected.StartAsync();
            Equivalent(MediumApiContractComparison.Compare(expected.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
            Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(expected), Descriptions(app)));
            foreach (var endpoint in Routes(expected))
            {
                var actual = Route(app, endpoint.RoutePattern.RawText!);
                Assert.Equal(Responses(endpoint), Responses(actual));
                Assert.Equal(endpoint.Metadata.GetMetadata<MethodInfo>()!.ReturnType, actual.Metadata.GetMetadata<MethodInfo>()!.ReturnType);
            }
        }

        Assert.Equal(8, Routes(app).Count());
        Assert.Equal(8, Descriptions(app).Operations.Count);
        Assert.Equal(0, Probe(app).Calls);
        var entry = Route(app, "/header-sets/entry/{kind}");
        Assert.Equal(typeof(HeaderSetEntryEndpoint), entry.Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        Assert.IsType<SampleNoCacheHeadersAttribute>(Assert.Single(entry.Metadata.GetOrderedMetadata<ResponseHeadersAttribute>()));
        Assert.Empty(entry.Metadata.GetOrderedMetadata<SampleNoStoreAttribute>());
        Assert.Empty(entry.Metadata.GetOrderedMetadata<SampleNoCachePragmaAttribute>());
        Assert.DoesNotContain(entry.Metadata, value => value is UnrelatedHeaderExampleAttribute);
    }

    [Theory]
    [InlineData("ok", 200)]
    [InlineData("notmodified", 304)]
    [InlineData("file", 200)]
    [InlineData("missing", 404)]
    public async Task Unconditional_handler_pairs_keep_native_values_payloads_file_headers_and_result_identity(string kind, int status)
    {
        await using var expected = Create("native");
        await expected.StartAsync();
        using var expectedClient = expected.GetTestClient();
        using var native = await expectedClient.GetAsync("/header-sets/entry/" + kind);
        foreach (var reference in new[] { "old", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/header-sets/entry/" + kind);
            await SameResponse(native, response);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
            Assert.Equal("no-cache", Assert.Single(response.Headers.GetValues("Pragma")));
            Assert.Equal("class", Assert.Single(response.Headers.GetValues("X-Class-First")));
            Assert.Equal("class", Assert.Single(response.Headers.GetValues("X-Class-Second")));
            Assert.Equal(1, Probe(app).Calls);
            Assert.Same(Probe(app).Returned, Probe(app).Observed);
            Assert.IsType<Results<Ok<HeaderSetReply>, StatusCodeHttpResult, FileContentHttpResult, NotFound>>(Probe(app).Observed);
        }
    }

    [Theory]
    [InlineData("ok", 200, true)]
    [InlineData("notmodified", 304, true)]
    [InlineData("file", 200, true)]
    [InlineData("missing", 404, false)]
    public async Task Selected_children_match_returned_types_including_304_and_file_without_matching_missing(string kind, int status, bool selected)
    {
        await using var expected = Create("native");
        await expected.StartAsync();
        using var expectedClient = expected.GetTestClient();
        using var native = await expectedClient.GetAsync("/header-sets/selected/" + kind);
        foreach (var reference in new[] { "old", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/header-sets/selected/" + kind);
            await SameResponse(native, response);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal(selected, response.Headers.Contains("Cache-Control"));
            Assert.Equal(selected, response.Headers.Contains("Pragma"));
            Assert.Same(Probe(app).Returned, Probe(app).Observed);
        }
    }

    [Theory]
    [InlineData("/header-sets/filter/ok")]
    [InlineData("/header-sets/factory/ok")]
    public async Task Registered_filter_and_static_factory_sets_keep_their_native_owner_boundary(string path)
    {
        await using var expected = Create("native");
        await expected.StartAsync();
        using var expectedClient = expected.GetTestClient();
        using var native = await expectedClient.GetAsync(path);
        foreach (var reference in new[] { "old", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            await SameResponse(native, response);
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
            Assert.Equal("no-cache", Assert.Single(response.Headers.GetValues("Pragma")));
            Assert.Equal(1, Probe(app).Calls);
            Assert.DoesNotContain(response.Headers, header => header.Key.Contains("Ignored", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Filter_short_circuit_retains_its_pair_and_prevents_handler_headers_and_execution()
    {
        foreach (var reference in new[] { "native", "old", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/header-sets/filter/ok?block=true");
            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
            Assert.Equal("no-cache", Assert.Single(response.Headers.GetValues("Pragma")));
            Assert.False(response.Headers.Contains("X-Handler"));
            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Earlier_guard_runs_before_namespace_pair_and_handler_headers()
    {
        foreach (var reference in new[] { "native", "old", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/header-sets/entry/ok");
            request.Headers.Add("X-Reject", "true");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(Headers(response));
            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Malformed_binding_keeps_outer_native_group_pair_and_skips_handler_pair()
    {
        await using var expected = Create("native");
        await expected.StartAsync();
        using var expectedClient = expected.GetTestClient();
        using var native = await expectedClient.GetAsync("/header-sets/entry/ok?limit=invalid");
        foreach (var reference in new[] { "old", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/header-sets/entry/ok?limit=invalid");
            await SameResponse(native, response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.True(response.Headers.Contains("X-Group-First"));
            Assert.True(response.Headers.Contains("X-Group-Second"));
            Assert.False(response.Headers.Contains("Cache-Control"));
            Assert.False(response.Headers.Contains("Pragma"));
            Assert.False(response.Headers.Contains("X-Class-First"));
            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Theory]
    [InlineData("fault")]
    [InlineData("cancel")]
    public async Task Exceptions_keep_entry_headers_and_skip_selected_post_result_children(string kind)
    {
        foreach (var reference in new[] { "native", "old", "generated" })
        {
            foreach (var selected in new[] { false, true })
            {
                await using var app = Create(reference);
                IHeaderDictionary? captured = null;
                app.Use(async (context, next) =>
                {
                    try { await next(context); }
                    finally { captured = new HeaderDictionary(context.Response.Headers.ToDictionary(pair => pair.Key, pair => pair.Value)); }
                });
                await app.StartAsync();
                using var client = app.GetTestClient();
                var path = "/header-sets/" + (selected ? "selected/" : "entry/") + kind;
                if (kind == "fault") { await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync(path)); }
                else { await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync(path)); }

                Assert.NotNull(captured);
                Assert.Equal(!selected, captured!.ContainsKey("Cache-Control"));
                Assert.Equal(!selected, captured.ContainsKey("Pragma"));
                Assert.True(captured.ContainsKey("X-Group-First"));
                Assert.Equal(1, Probe(app).Calls);
            }
        }
    }

    [Fact]
    public async Task Native_method_inheritance_keeps_mixed_declarations_and_direct_class_boundaries()
    {
        foreach (var path in new[] { "/header-sets/inherited/ok", "/header-sets/override/ok" })
        {
            var oldType = path.Contains("/override/", StringComparison.Ordinal) ? typeof(OldOverrideHeaderSetEndpoint) : typeof(OldInheritedHeaderSetEndpoint);
            var oldDeclarations = oldType.GetMethod("HandleAsync")!.GetCustomAttributes<ResponseHeaderAttribute>(inherit: true).ToArray();
            Assert.All(oldDeclarations, header =>
            {
                Assert.Null(header.ResultType);
                Assert.Null(header.ResultTypes);
            });
            var expectedInherited = oldDeclarations.Last(header => header.Name == "X-Inherited").Values.Single();
            await using var native = Create("native");
            await native.StartAsync();
            using var expectedClient = native.GetTestClient();
            using var expected = await expectedClient.GetAsync(path);
            foreach (var reference in new[] { "old", "generated" })
            {
                await using var app = Create(reference);
                await app.StartAsync();
                using var client = app.GetTestClient();
                using var response = await client.GetAsync(path);
                await SameResponse(expected, response);
                Assert.Equal(expectedInherited, Assert.Single(response.Headers.GetValues("X-Inherited")));
                Assert.False(response.Headers.Contains("X-Ignored-Base-Class"));
            }
        }

        await using var usageApp = Create(map: false);
        usageApp.MapMediumApiEndpoint<NativeUsageEndpoint>();
        await usageApp.StartAsync();
        var method = typeof(NativeUsageEndpoint).GetMethod("HandleAsync")!;
        var nativeDeclarations = method.GetCustomAttributes<ResponseHeaderDeclarationAttribute>(inherit: true).ToArray();
        Assert.DoesNotContain(nativeDeclarations, declaration => declaration is LocalOnlySetAttribute);
        Assert.Equal("derived", Assert.Single(nativeDeclarations.OfType<UniqueInheritedSetAttribute>()).Headers[0].Values[0]);
        Assert.Single(nativeDeclarations.OfType<ChildUsageSetAttribute>());
        using var usageClient = usageApp.GetTestClient();
        using var actual = await usageClient.GetAsync("/usage");
        Assert.False(actual.Headers.Contains("X-Local-Only"));
        Assert.Equal("derived", Assert.Single(actual.Headers.GetValues("X-Unique")));
        Assert.Equal("retained-as-object", Assert.Single(actual.Headers.GetValues("X-Child-Usage")));
        var nativeLastMixed = nativeDeclarations.OfType<ResponseHeaderAttribute>().Where(header => header.Name == "X-Mixed").Last().Values[0];
        Assert.Equal(nativeLastMixed, Assert.Single(actual.Headers.GetValues("X-Mixed")));
    }

    [Fact]
    public async Task Mixed_handler_sets_keep_forward_order_while_individual_fluent_children_unwind_in_reverse()
    {
        await using var app = Create(map: false);
        app.MapMediumApiEndpoint<OrderedHeaderSetEndpoint>();
        var grouped = MapOriginal(app, "/grouped", HeaderSetBodies.Fluent).WithResponseHeaders(new OrderedHeadersAttribute());
        var old = MapOriginal(app, "/old", HeaderSetBodies.Fluent);
        foreach (var header in new OrderedHeadersAttribute().Headers) { old.WithResponseHeader(header); }

        var native = MapOriginal(app, "/native", HeaderSetBodies.Fluent);
        native.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers["X-Order"] = "inside-first";
            context.HttpContext.Response.Headers["Cache-Control"] = "first";
            var result = await next(context);
            if (result is Ok<HeaderSetReply>) { context.HttpContext.Response.Headers["X-Selected"] = "selected-first"; }

            return result;
        });
        native.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers["X-Order"] = "inside-last";
            context.HttpContext.Response.Headers["cache-control"] = "last";
            var result = await next(context);
            if (result is Ok<HeaderSetReply>) { context.HttpContext.Response.Headers["X-Selected"] = "selected-last"; }

            return result;
        });
        Assert.NotNull(grouped);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var handler = await client.GetAsync("/ordered");
        Assert.Equal("after", Assert.Single(handler.Headers.GetValues("X-Order")));
        Assert.Equal("selected-last", Assert.Single(handler.Headers.GetValues("X-Selected")));
        Assert.Equal("last", Assert.Single(handler.Headers.GetValues("Cache-Control")));
        using var expected = await client.GetAsync("/native");
        foreach (var path in new[] { "/grouped", "/old" })
        {
            using var actual = await client.GetAsync(path);
            await SameResponse(expected, actual);
            Assert.Equal("inside-last", Assert.Single(actual.Headers.GetValues("X-Order")));
            Assert.Equal("selected-first", Assert.Single(actual.Headers.GetValues("X-Selected")));
            Assert.Equal("last", Assert.Single(actual.Headers.GetValues("Cache-Control")));
        }
    }

    [Fact]
    public async Task Fluent_mapping_snapshots_each_child_selector_and_keeps_fresh_mounts_independent()
    {
        var child = new SampleHeaderAttribute("X-Snapshot", "retained") { ResultType = typeof(Ok<>) };
        var input = new List<ResponseHeaderAttribute> { child };
        var set = new HeaderListAttribute(input);
        input[0] = new SampleHeaderAttribute("X-Replaced", "unused");
        await using var first = Create(map: false);
        await using var second = Create(map: false);
        var mapped = MapOriginal(first, "/value", HeaderSetBodies.Fluent);
        Assert.Same(mapped, mapped.WithResponseHeaders(set));
        child.ResultType = typeof(NotFound);
        MapOriginal(second, "/value", HeaderSetBodies.Fluent).WithResponseHeaders(set);
        child.ResultType = typeof(Ok<>);
        await first.StartAsync();
        await second.StartAsync();
        using var firstClient = first.GetTestClient();
        using var secondClient = second.GetTestClient();
        using var firstResponse = await firstClient.GetAsync("/value");
        using var secondResponse = await secondClient.GetAsync("/value");
        Assert.Equal("retained", Assert.Single(firstResponse.Headers.GetValues("X-Snapshot")));
        Assert.False(secondResponse.Headers.Contains("X-Snapshot"));
        Assert.False(firstResponse.Headers.Contains("X-Replaced"));
        Assert.False(secondResponse.Headers.Contains("X-Replaced"));
    }

    [Fact]
    public void Fluent_empty_null_and_later_invalid_children_preserve_old_sequential_registration_contract()
    {
        var builder = new CountingConventionBuilder();
        var empty = new HeaderListAttribute(Array.Empty<ResponseHeaderAttribute>());
        Assert.Same(builder, builder.WithResponseHeaders(empty));
        Assert.Equal(0, builder.Count);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => ((CountingConventionBuilder)null!).WithResponseHeaders(null!)).ParamName);
        Assert.Equal("headers", Assert.Throws<ArgumentNullException>(() => builder.WithResponseHeaders(null!)).ParamName);
        var invalid = new SampleHeaderAttribute("X-Invalid", "late") { ResultType = typeof(Ok<>), ResultTypes = new[] { typeof(Ok<>) } };
        var set = new HeaderListAttribute(new ResponseHeaderAttribute[] { new SampleHeaderAttribute("X-First", "registered"), invalid });
        Assert.Throws<InvalidOperationException>(() => builder.WithResponseHeaders(set));
        Assert.Equal(1, builder.Count);
    }

    [Fact]
    public async Task Aliases_disabled_groups_public_selection_and_default_routes_remain_independent()
    {
        await using var disabled = Create(enabled: false);
        Assert.Null(disabled.TryMapMediumApiNamespace<DisabledHeaderSetsGroupDefinition>());
        await disabled.StartAsync();
        Assert.Empty(Routes(disabled));
        foreach (var reference in new[] { "native", "old", "generated" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            Assert.Equal("HeaderSetAlias", Route(app, "/header-sets/alias/{kind}").Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
            using var client = app.GetTestClient();
            using var alias = await client.GetAsync("/header-sets/alias/ok");
            using var fluent = await client.GetAsync("/header-sets/fluent");
            Assert.Equal("no-store", alias.Headers.CacheControl?.ToString());
            Assert.Equal("no-cache", Assert.Single(alias.Headers.GetValues("Pragma")));
            Assert.Equal("no-store", fluent.Headers.CacheControl?.ToString());
            Assert.Equal("no-cache", Assert.Single(fluent.Headers.GetValues("Pragma")));
        }

        await using var selected = Program.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration["Sample:HeaderSets"] = "true";
        });
        await using var original = Program.Build(configure: builder => builder.WebHost.UseTestServer());
        await selected.StartAsync();
        await original.StartAsync();
        Assert.Equal(8, Routes(selected).Count());
        Assert.DoesNotContain(Routes(original), endpoint => endpoint.RoutePattern.RawText!.StartsWith("/header-sets", StringComparison.Ordinal));
        using var selectedClient = selected.GetTestClient();
        using var response = await selectedClient.GetAsync("/header-sets/entry/ok");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal("no-cache", Assert.Single(response.Headers.GetValues("Pragma")));
    }

    private static string[] Responses(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(response => response.StatusCode + ":" + response.Type?.AssemblyQualifiedName + ":" + string.Join(",", response.ContentTypes)).ToArray();
    }
}