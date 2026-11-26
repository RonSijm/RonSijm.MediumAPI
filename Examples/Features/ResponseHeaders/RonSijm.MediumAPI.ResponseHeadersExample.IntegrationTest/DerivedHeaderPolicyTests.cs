using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using RonSijm.MediumAPI.ResponseHeadersExample.DerivedPolicies.Disabled;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.ResponseHeadersExample.Program;

namespace RonSijm.MediumAPI.ResponseHeadersExample.IntegrationTest;

public sealed class DerivedHeaderPolicyTests
{
    [Theory]
    [InlineData("ok", 200)]
    [InlineData("created", 201)]
    [InlineData("missing", 404)]
    public async Task Derived_class_and_method_headers_keep_exact_native_nested_result_boundaries(string kind, int status)
    {
        await using var native = Create(native: true);
        await using var app = Create();
        await native.StartAsync();
        await app.StartAsync();
        using var nativeClient = native.GetTestClient();
        using var client = app.GetTestClient();
        using var expected = await nativeClient.GetAsync("/derived/items/" + kind);
        using var actual = await client.GetAsync("/derived/items/" + kind);
        await SameResponse(expected, actual);
        Assert.Equal(status, (int)actual.StatusCode);
        Assert.Equal("constant", Value(actual, "X-Class"));
        Assert.Equal("no-cache", Value(actual, "Pragma"));
        Assert.Equal(kind != "missing", actual.Headers.Contains("Cache-Control"));
        Assert.Equal(kind != "missing", actual.Headers.Contains("X-Derived-Group-Selected"));
        Assert.Equal(kind == "created", actual.Headers.Contains("X-Method"));
        if (kind == "ok") { Assert.Equal(new[] { "one", "two" }, actual.Headers.GetValues("X-Values")); }
        else { Assert.False(actual.Headers.Contains("X-Values")); }

        var probe = app.Services.GetRequiredService<DerivedHeaderProbe>();
        Assert.Equal(1, probe.Calls);
        Assert.Same(probe.Returned, probe.Observed);
        Assert.IsType<Results<Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>>, NotFound>>(probe.Observed);
    }

    [Fact]
    public async Task Native_result_metadata_API_Explorer_names_and_alias_contracts_are_unchanged()
    {
        await using var native = Create(native: true);
        await using var app = Create();
        await native.StartAsync();
        await app.StartAsync();
        var comparison = MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot());
        Equivalent(comparison);
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(native.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot(), app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot()));
        Assert.Equal(7, Routes(app).Count());
        foreach (var expected in Routes(native))
        {
            var actual = Route(app, expected.RoutePattern.RawText!);
            Assert.Equal(Responses(expected), Responses(actual));
            Assert.Equal(expected.Metadata.GetMetadata<MethodInfo>()!.ReturnType, actual.Metadata.GetMetadata<MethodInfo>()!.ReturnType);
        }

        Assert.Null(Route(app, "/derived/alias/{kind}").Metadata.GetMetadata<IEndpointNameMetadata>());
        Assert.Equal(0, app.Services.GetRequiredService<DerivedHeaderProbe>().Calls);
    }

    [Theory]
    [InlineData("/derived/filtered/ok")]
    [InlineData("/derived/factory/ok")]
    [InlineData("/derived/inherited/ok")]
    [InlineData("/derived/fluent")]
    [InlineData("/derived/convention")]
    public async Task Filter_factory_inherited_method_fluent_and_startup_conventions_match_native_policies(string path)
    {
        await using var native = Create(native: true);
        await using var app = Create();
        await native.StartAsync();
        await app.StartAsync();
        using var nativeClient = native.GetTestClient();
        using var client = app.GetTestClient();
        using var expected = await nativeClient.GetAsync(path);
        using var actual = await client.GetAsync(path);
        await SameResponse(expected, actual);
        Assert.Equal("group", Value(actual, "X-Derived-Group"));
        Assert.False(actual.Headers.Contains("X-Ignored-Endpoint-Base"));
        Assert.False(actual.Headers.Contains("X-Ignored-Group-Base"));
        Assert.False(actual.Headers.Contains("X-Ignored-Filter-Base"));
        if (path.Contains("/inherited/", StringComparison.Ordinal)) { Assert.Equal("inherited", Value(actual, "X-Inherited-Method")); }

        if (path.EndsWith("/fluent", StringComparison.Ordinal)) { Assert.Equal(new[] { "first", "second" }, actual.Headers.GetValues("X-Fluent")); }

        if (path.EndsWith("/convention", StringComparison.Ordinal)) { Assert.Equal("startup", Value(actual, "X-Convention")); }
    }

    [Fact]
    public async Task Earlier_native_guard_rejects_before_namespace_and_handler_headers()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/derived/items/ok");
            request.Headers.Add("X-Sample-Reject", "true");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(PolicyHeaders(response));
            Assert.Equal(0, app.Services.GetRequiredService<DerivedHeaderProbe>().Calls);
        }
    }

    [Fact]
    public async Task Malformed_binding_runs_only_native_outer_policies_and_skips_handler_declarations()
    {
        await using var native = Create(native: true);
        await using var app = Create();
        await native.StartAsync();
        await app.StartAsync();
        using var nativeClient = native.GetTestClient();
        using var client = app.GetTestClient();
        using var expected = await nativeClient.GetAsync("/derived/items/ok?limit=not-an-integer");
        using var actual = await client.GetAsync("/derived/items/ok?limit=not-an-integer");
        await SameResponse(expected, actual);
        Assert.Equal(HttpStatusCode.BadRequest, actual.StatusCode);
        Assert.Equal("group", Value(actual, "X-Derived-Group"));
        Assert.False(actual.Headers.Contains("Cache-Control"));
        Assert.False(actual.Headers.Contains("Pragma"));
        Assert.False(actual.Headers.Contains("X-Class"));
        Assert.Equal(0, app.Services.GetRequiredService<DerivedHeaderProbe>().Calls);
    }

    [Theory]
    [InlineData("fault")]
    [InlineData("cancel")]
    public async Task Unconditional_headers_survive_failures_but_result_selected_headers_are_not_added(string kind)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            IHeaderDictionary? captured = null;
            app.Use(async (context, next) =>
            {
                try { await next(context); }
                finally { captured = new HeaderDictionary(context.Response.Headers.ToDictionary(pair => pair.Key, pair => pair.Value)); }
            });
            await app.StartAsync();
            using var client = app.GetTestClient();
            if (kind == "cancel") { await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync("/derived/items/" + kind)); }
            else { await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/derived/items/" + kind)); }

            Assert.Equal("no-cache", captured!["Pragma"].ToString());
            Assert.Equal("constant", captured["X-Class"].ToString());
            Assert.Equal("group", captured["X-Derived-Group"].ToString());
            Assert.False(captured.ContainsKey("Cache-Control"));
            Assert.False(captured.ContainsKey("X-Values"));
            Assert.False(captured.ContainsKey("X-Derived-Group-Selected"));
        }
    }

    [Fact]
    public async Task A_registered_filter_can_return_selected_created_before_the_handler_without_gaining_handler_headers()
    {
        await using var native = Create(native: true);
        await using var app = Create();
        await native.StartAsync();
        await app.StartAsync();
        using var nativeClient = native.GetTestClient();
        using var client = app.GetTestClient();
        using var expected = await nativeClient.GetAsync("/derived/filtered/ok?filterBlock=true");
        using var actual = await client.GetAsync("/derived/filtered/ok?filterBlock=true");
        await SameResponse(expected, actual);
        Assert.Equal(HttpStatusCode.Created, actual.StatusCode);
        Assert.Equal("no-store", Value(actual, "Cache-Control"));
        Assert.Equal("filtered", Value(actual, "X-Filter"));
        Assert.False(actual.Headers.Contains("X-Handler-Only"));
        Assert.Equal(0, app.Services.GetRequiredService<DerivedHeaderProbe>().Calls);
    }

    [Fact]
    public async Task Disabled_namespace_does_not_construct_derived_attributes_and_default_host_keeps_existing_routes()
    {
        await using var app = Create(enabled: false);
        Assert.Null(app.TryMapMediumApiNamespace<DisabledDerivedHeaderGroupDefinition>());
        await app.StartAsync();
        Assert.Empty(Routes(app));
        await using var original = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await original.StartAsync();
        Assert.DoesNotContain(Routes(original), endpoint => endpoint.RoutePattern.RawText!.StartsWith("/derived", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Bad Name")]
    [InlineData("Bad\r\nName")]
    public void Derived_constructors_keep_base_header_name_validation_and_argument_identity(string name)
    {
        var expected = Assert.Throws<ArgumentException>(() => new ResponseHeaderAttribute(name, "value"));
        var actual = Assert.Throws<ArgumentException>(() => new SampleHeaderAttribute(name, "value"));
        Assert.Equal(expected.ParamName, actual.ParamName);
    }

    [Fact]
    public void Fixed_names_custom_values_and_inherited_selectors_keep_base_validation_and_copies()
    {
        Assert.Equal("Cache-Control", new SampleNoStoreAttribute().Name);
        Assert.Equal("no-store", Assert.Single(new SampleNoStoreAttribute().Values));
        Assert.Equal("Pragma", new SampleNoCachePragmaAttribute().Name);
        Assert.Equal("no-cache", Assert.Single(new SampleNoCachePragmaAttribute().Values));
        var values = new[] { "one", "two" };
        var selectors = new[] { typeof(Ok<>), typeof(Created<>) };
        var declaration = new SampleHeaderAttribute("X-Copied", values) { ResultTypes = selectors };
        values[0] = "changed";
        selectors[0] = typeof(NotFound);
        declaration.ResultTypes![1] = typeof(NotFound);
        Assert.Equal(new[] { "one", "two" }, declaration.Values);
        Assert.Equal(new[] { typeof(Ok<>), typeof(Created<>) }, declaration.ResultTypes);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)declaration.Values)[0] = "changed");
        Assert.Throws<ArgumentException>(() => new SampleHeaderAttribute("X-Value", "bad\nvalue"));
        Assert.Throws<ArgumentException>(() => new SampleSelectedHeaderAttribute(typeof(string)));
        Assert.Throws<ArgumentException>(() => declaration.ResultTypes = Array.Empty<Type>());
        Assert.Throws<ArgumentException>(() => declaration.ResultType = typeof(string));
    }

    [Fact]
    public async Task Fluent_snapshots_of_mutable_derived_selectors_remain_independent_per_host_and_alias()
    {
        await using var first = Create(enabled: false);
        await using var second = Create(enabled: false);
        var declaration = new SampleHeaderAttribute("X-Owned", "first", "second") { ResultTypes = new[] { typeof(Ok<>), typeof(Created<>) } };
        first.MapGet("/snapshot", () => TypedResults.Ok("fixed")).WithResponseHeader(declaration);
        declaration.ResultTypes = new[] { typeof(NotFound) };
        second.MapGet("/snapshot", () => TypedResults.Ok("fixed")).WithResponseHeader(declaration);
        declaration.ResultTypes = null;
        await first.StartAsync();
        await second.StartAsync();
        using var firstClient = first.GetTestClient();
        using var secondClient = second.GetTestClient();
        using var included = await firstClient.GetAsync("/snapshot");
        using var excluded = await secondClient.GetAsync("/snapshot");
        Assert.Equal(new[] { "first", "second" }, included.Headers.GetValues("X-Owned"));
        Assert.False(excluded.Headers.Contains("X-Owned"));
    }

    [Fact]
    public async Task Conflicting_inherited_selectors_fail_at_registration_and_constructor_selection_still_works()
    {
        await using var app = Create(enabled: false);
        var declaration = new SampleSelectedHeaderAttribute(typeof(Ok<>)) { ResultTypes = new[] { typeof(Created<>) } };
        var endpoint = app.MapGet("/selector", () => TypedResults.Ok("fixed"));
        Assert.Throws<InvalidOperationException>(() => endpoint.WithResponseHeader(declaration));
        declaration.ResultTypes = null;
        endpoint.WithResponseHeader(declaration);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/selector");
        Assert.Equal("selected", Value(response, "X-Constructor-Selected"));
    }

    [Fact]
    public async Task Manual_alias_retains_the_same_declared_policy_and_no_fallback_name()
    {
        await using var app = Create();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var original = await client.GetAsync("/derived/items/ok");
        using var alias = await client.GetAsync("/derived/alias/ok");
        await SameResponse(original, alias);
        Assert.Null(Route(app, "/derived/alias/{kind}").Metadata.GetMetadata<IEndpointNameMetadata>());
    }

    private static WebApplication Create(bool native = false, bool enabled = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(ExampleProgram).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<DerivedHeaderProbe>();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Sample:DerivedHeaders"] = enabled.ToString() });
        var app = builder.Build();
        app.UseRouting();
        if (native) { MapNative(app, enabled); }
        else { DerivedHeaderSample.Map(app); }

        return app;
    }

    private static async Task SameResponse(HttpResponseMessage expected, HttpResponseMessage actual)
    {
        Assert.Equal(expected.StatusCode, actual.StatusCode);
        Assert.Equal(expected.Headers.Location, actual.Headers.Location);
        Assert.Equal(expected.Content.Headers.ContentType?.ToString(), actual.Content.Headers.ContentType?.ToString());
        Assert.Equal(PolicyHeaders(expected), PolicyHeaders(actual));
        Assert.Equal(await expected.Content.ReadAsByteArrayAsync(), await actual.Content.ReadAsByteArrayAsync());
    }

    private static string[] PolicyHeaders(HttpResponseMessage response)
    {
        return response.Headers.Where(pair => pair.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase) || pair.Key.Equals("Cache-Control", StringComparison.OrdinalIgnoreCase) || pair.Key.Equals("Pragma", StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Key + ":" + string.Join("|", pair.Value)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private static string Value(HttpResponseMessage response, string name)
    {
        return Assert.Single(response.Headers.GetValues(name));
    }

    private static IEnumerable<RouteEndpoint> Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    }

    private static RouteEndpoint Route(WebApplication app, string pattern)
    {
        return Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == pattern);
    }

    private static string[] Responses(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(value => value.StatusCode + ":" + value.Type?.AssemblyQualifiedName + ":" + string.Join(",", value.ContentTypes)).ToArray();
    }

    private static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(value => value.Kind + " " + value.Path)));
    }

    private static void MapNative(WebApplication app, bool enabled)
    {
        if (!enabled) { return; }

        var group = app.MapGroup("/derived").WithTags("Derived headers").AddEndpointFilter<DerivedHeaderObserver>().AddEndpointFilter<DerivedHeaderGuard>();
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers["X-Derived-Group"] = "group";
            var result = await next(context);
            if (NativeSuccess(result)) { context.HttpContext.Response.Headers["X-Derived-Group-Selected"] = "selected"; }

            return result;
        });
        group.MapGet("/items/{kind}", NativeEntry).WithName("DerivedEntry");
        group.MapGet("/alias/{kind}", NativeEntry);
        group.MapGet("/filtered/{kind}", NativeFiltered).WithName("DerivedFilter").AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers["X-Filter"] = "filtered";
            var result = context.HttpContext.Request.Query.ContainsKey("filterBlock") ? TypedResults.Created("/derived/items/blocked", new DerivedHeaderReply("blocked")) : await next(context);
            if (NativeSuccess(result)) { context.HttpContext.Response.Headers.CacheControl = "no-store"; }

            return result;
        });
        group.MapGet("/factory/{kind}", NativeFactory).WithName("DerivedFactory").AddEndpointFilterFactory((_, next) => context =>
        {
            context.HttpContext.Response.Headers["X-Factory"] = "factory";
            context.HttpContext.Response.Headers.Pragma = "no-cache";
            return next(context);
        });
        group.MapGet("/inherited/{kind}", NativeInherited).WithName("InheritedDerived");
        group.MapGet("/fluent", () => TypedResults.Ok(new DerivedHeaderReply("fluent"))).WithName("DerivedFluent").AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers["X-Fluent"] = new StringValues(new[] { "first", "second" });
            var result = await next(context);
            if (result is Ok<DerivedHeaderReply>) { context.HttpContext.Response.Headers.CacheControl = "no-store"; }

            return result;
        });
        group.MapGet("/convention", () => TypedResults.Ok(new DerivedHeaderReply("convention"))).WithName("DerivedConvention").AddEndpointFilter((context, next) =>
        {
            context.HttpContext.Response.Headers.Pragma = "no-cache";
            context.HttpContext.Response.Headers["X-Convention"] = "startup";
            return next(context);
        });
    }

    private static Results<Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>>, NotFound> NativeEntry([FromRoute] string kind, [FromQuery] int? limit, HttpContext context, DerivedHeaderProbe probe)
    {
        context.Response.Headers["X-Class"] = "constant";
        context.Response.Headers.Pragma = "no-cache";
        var result = DerivedHeaderBodies.Create(kind, probe);
        if (NativeSuccess(result)) { context.Response.Headers.CacheControl = "no-store"; }

        if (result.Result is Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>> success)
        {
            if (success.Result is Ok<DerivedHeaderReply>) { context.Response.Headers["X-Values"] = new StringValues(new[] { "one", "two" }); }

            if (success.Result is Created<DerivedHeaderReply>) { context.Response.Headers["X-Method"] = "selected"; }
        }

        return result;
    }

    private static Results<Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>>, NotFound> NativeFiltered([FromRoute] string kind, HttpContext context, DerivedHeaderProbe probe)
    {
        context.Response.Headers["X-Handler-Only"] = "handler";
        return DerivedHeaderBodies.Create(kind, probe);
    }

    private static Results<Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>>, NotFound> NativeFactory([FromRoute] string kind, DerivedHeaderProbe probe)
    {
        return DerivedHeaderBodies.Create(kind, probe);
    }

    private static Results<Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>>, NotFound> NativeInherited([FromRoute] string kind, HttpContext context, DerivedHeaderProbe probe)
    {
        context.Response.Headers["X-Inherited-Method"] = "inherited";
        context.Response.Headers.Pragma = "no-cache";
        return DerivedHeaderBodies.Create(kind, probe);
    }

    private static bool NativeSuccess(object? result)
    {
        if (result is Results<Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>>, NotFound> outer) { result = outer.Result; }

        if (result is Results<Ok<DerivedHeaderReply>, Created<DerivedHeaderReply>> inner) { result = inner.Result; }

        return result is Ok<DerivedHeaderReply> or Created<DerivedHeaderReply>;
    }
}