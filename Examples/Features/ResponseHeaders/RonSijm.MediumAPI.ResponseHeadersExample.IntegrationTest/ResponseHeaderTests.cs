using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.ResponseHeadersExample.Endpoints.Disabled;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.ResponseHeadersExample.Program;

namespace RonSijm.MediumAPI.ResponseHeadersExample.IntegrationTest;

public sealed class ResponseHeaderTests
{
    [Theory]
    [InlineData(1, HttpStatusCode.OK)]
    [InlineData(0, HttpStatusCode.NotFound)]
    public async Task Handler_defaults_apply_to_each_explicit_outcome_and_use_wire_header_names(int id, HttpStatusCode status)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/items/" + id);
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.False(response.Headers.Contains("CacheControl"));
        Assert.Equal("item", Header(response, "X-Handler"));
        Assert.Equal("api", Header(response, "X-Group"));
    }

    [Fact]
    public async Task Method_declarations_override_class_defaults_and_the_handler_can_override_a_default()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/override");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("method", Header(response, "X-Policy"));
        Assert.Equal("handler", Header(response, "X-Default"));
    }

    [Theory]
    [InlineData("ok", HttpStatusCode.OK, "ok", false)]
    [InlineData("file", HttpStatusCode.OK, "handler", true)]
    [InlineData("missing", HttpStatusCode.NotFound, "default", false)]
    public async Task Result_conditions_unwrap_nested_unions_and_apply_after_the_handler(string kind, HttpStatusCode status, string selected, bool download)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/conditional/" + kind);
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(selected, Header(response, "X-Selected"));
        Assert.Equal(download, response.Headers.Contains("X-Download"));
        Assert.Equal(download, response.Headers.Contains("Content-Security-Policy"));
        Assert.Equal(download, response.Headers.Contains("X-Content-Type-Options"));
        if (download)
        {
            Assert.Equal("yes", Header(response, "X-Download"));
            Assert.Equal("sandbox", Header(response, "Content-Security-Policy"));
            Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
            Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("sample download", await response.Content.ReadAsStringAsync());
            Assert.Contains("sample.txt", response.Content.Headers.ContentDisposition?.ToString());
        }
    }

    [Theory]
    [InlineData("fault")]
    [InlineData("cancel")]
    public async Task Handler_faults_and_cancellation_propagate_without_result_selected_headers(string kind)
    {
        await using var app = CreateHost();
        IHeaderDictionary? captured = null;
        app.Use(async (context, next) =>
        {
            try { await next(context); }
            finally { captured = new HeaderDictionary(context.Response.Headers.ToDictionary(pair => pair.Key, pair => pair.Value)); }
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        if (kind == "cancel")
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync("/api/conditional/" + kind));
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/api/conditional/" + kind));
        }

        Assert.NotNull(captured);
        Assert.Equal("default", captured["X-Selected"].ToString());
        Assert.False(captured.ContainsKey("X-Download"));
    }

    [Theory]
    [InlineData("/api/filtered")]
    [InlineData("/api/fluent-filtered")]
    public async Task Filter_attributes_and_Configure_headers_are_applied_once_at_registration(string path)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("attribute", Header(response, "X-Filter"));
        Assert.Equal("configured", Header(response, "X-Configured"));
        Assert.Equal("handler", Header(response, "X-Handler-Only"));
        Assert.False(response.Headers.Contains("X-Filter-Base"));
        Assert.False(response.Headers.Contains("X-Filter-Outcome"));
        Assert.Single(Endpoint(app, path).Metadata.GetOrderedMetadata<HeaderConfigurationMarker>());
    }

    [Theory]
    [InlineData("/api/filtered")]
    [InlineData("/api/fluent-filtered")]
    public async Task A_filter_short_circuit_gets_its_own_policy_but_not_the_handlers_policy(string path)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync(path + "?block=true");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("attribute", Header(response, "X-Filter"));
        Assert.Equal("configured", Header(response, "X-Configured"));
        Assert.Equal("blocked", Header(response, "X-Filter-Outcome"));
        Assert.Equal("api", Header(response, "X-Group"));
        Assert.False(response.Headers.Contains("X-Handler-Only"));
    }

    [Fact]
    public async Task An_ordinary_short_circuit_does_not_receive_handler_headers()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/short-circuit");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("api", Header(response, "X-Group"));
        Assert.False(response.Headers.Contains("X-Handler-Only"));
    }

    [Fact]
    public async Task An_outer_exception_filter_keeps_the_started_handler_defaults_without_inventing_a_success_selection()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/filtered?recover=true");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("no-store", Header(response, "Cache-Control"));
        Assert.Equal("handler", Header(response, "X-Handler-Only"));
        Assert.Equal("blocked", Header(response, "X-Filter-Outcome"));
        Assert.False(response.Headers.Contains("X-Handler-Success"));
    }

    [Theory]
    [InlineData("/api/bind/not-an-int", HttpStatusCode.BadRequest, true)]
    [InlineData("/api/protected", HttpStatusCode.Unauthorized, false)]
    public async Task Binding_and_authorization_failures_skip_handler_headers_at_their_native_boundaries(string path, HttpStatusCode status, bool groupRuns)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(status, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Handler-Only"));
        // Native filters still run on a parse failure, while authorization middleware
        // short-circuits before the endpoint filter pipeline is entered.
        Assert.Equal(groupRuns, response.Headers.Contains("X-Group"));
        if (groupRuns)
        {
            Assert.Equal("api", Header(response, "X-Group"));
        }
    }

    [Fact]
    public async Task A_native_filter_error_status_skips_the_handler_and_its_headers_even_when_next_is_called()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/status-guard");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("api", Header(response, "X-Group"));
        Assert.False(response.Headers.Contains("X-Handler-Only"));
    }

    [Theory]
    [InlineData("/api/native", false)]
    [InlineData("/api/child/value", true)]
    public async Task Namespace_policies_cover_native_routes_and_nested_groups(string path, bool child)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("api", Header(response, "X-Group"));
        Assert.Equal(child, response.Headers.Contains("X-Child"));
        if (child)
        {
            Assert.Equal("child", Header(response, "X-Child"));
        }
    }

    [Theory]
    [InlineData("/fluent", "endpoint")]
    [InlineData("/fluent-group/value", "group")]
    public async Task Native_endpoint_and_group_builders_accept_the_same_header_convention(string path, string value)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(value, Header(response, "X-Fluent"));
    }

    [Fact]
    public async Task Header_policies_leave_typed_response_metadata_and_named_links_unchanged()
    {
        await using var app = CreateHost();
        app.MapGet("/metadata-twin", () => TypedResults.Ok(new HeaderItem(1, "twin")));
        await app.StartAsync();
        var fluentMetadata = Endpoint(app, "/fluent").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        var twinMetadata = Endpoint(app, "/metadata-twin").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        Assert.Equal(twinMetadata.Select(Describe), fluentMetadata.Select(Describe));
        Assert.Equal("/api/items/42", app.Services.GetRequiredService<LinkGenerator>().GetPathByName("GetItem", new { id = 42 }));
        Assert.Equal("/fluent", app.Services.GetRequiredService<LinkGenerator>().GetPathByName("NativeHeaders", values: null));
    }

    [Fact]
    public async Task Inherited_method_headers_are_retained_while_base_class_headers_are_not_imported()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/api/inherited");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("method", Header(response, "X-Inherited-Method"));
        Assert.False(response.Headers.Contains("X-Base-Class"));
    }

    [Fact]
    public async Task Multi_value_policies_copy_the_supplied_array_and_allow_an_empty_string_value()
    {
        await using var app = CreateHost();
        var values = new[] { "first", "second" };
        app.MapGet("/values", () => TypedResults.Ok()).WithResponseHeader(new ResponseHeaderAttribute("X-Values", values)).WithResponseHeader("X-Empty", "");
        values[0] = "mutated after registration";
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/values");
        Assert.Equal(new[] { "first", "second" }, response.Headers.GetValues("X-Values"));
        Assert.True(response.Headers.TryGetValues("X-Empty", out var empty));
        Assert.Equal("", string.Join("", empty));
    }

    [Fact]
    public async Task Mutating_a_requests_multi_value_header_array_does_not_change_later_requests()
    {
        await using var app = CreateHost();
        app.MapGet("/request-values", (HttpContext context) =>
        {
            if (context.Request.Query.ContainsKey("mutate"))
            {
                string[]? requestValues = context.Response.Headers["X-Multi"];
                requestValues![0] = "request mutation";
            }

            return TypedResults.Ok();
        }).WithResponseHeader(new ResponseHeaderAttribute("X-Multi", "first", "second"));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var mutated = await client.GetAsync("/request-values?mutate=true");
        using var subsequent = await client.GetAsync("/request-values");
        Assert.Equal(HttpStatusCode.OK, mutated.StatusCode);
        Assert.Equal(new[] { "request mutation", "second" }, mutated.Headers.GetValues("X-Multi"));
        Assert.Equal(new[] { "first", "second" }, subsequent.Headers.GetValues("X-Multi"));
    }

    [Fact]
    public async Task Closed_result_type_conditions_apply_only_to_the_matching_concrete_result()
    {
        await using var app = CreateHost();
        app.MapGet("/exact", () => TypedResults.Ok(new HeaderItem(1, "exact"))).WithResponseHeader("X-Exact", "matched", typeof(Ok<HeaderItem>));
        app.MapGet("/different", () => TypedResults.Ok("other payload")).WithResponseHeader("X-Exact", "matched", typeof(Ok<HeaderItem>));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var matched = await client.GetAsync("/exact");
        using var other = await client.GetAsync("/different");
        Assert.Equal("matched", Header(matched, "X-Exact"));
        Assert.False(other.Headers.Contains("X-Exact"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Bad Name")]
    [InlineData("Bad:Name")]
    [InlineData("Bad\rName")]
    public async Task Invalid_header_names_are_rejected_before_a_request(string name)
    {
        await using var app = CreateHost();
        Assert.ThrowsAny<ArgumentException>(() => app.MapGet("/invalid-name", () => TypedResults.Ok()).WithResponseHeader(name, "value"));
    }

    [Theory]
    [InlineData("bad\rvalue")]
    [InlineData("bad\nvalue")]
    [InlineData("bad\0value")]
    [InlineData("bad\u0001value")]
    [InlineData("bad\u007fvalue")]
    public async Task Invalid_header_values_are_rejected_before_a_request(string value)
    {
        await using var app = CreateHost();
        Assert.ThrowsAny<ArgumentException>(() => app.MapGet("/invalid-value", () => TypedResults.Ok()).WithResponseHeader("X-Test", value));
    }

    [Fact]
    public async Task Null_empty_collection_and_non_result_conditions_are_rejected_at_registration()
    {
        await using var app = CreateHost();
        Assert.ThrowsAny<ArgumentException>(() => app.MapGet("/null-name", () => TypedResults.Ok()).WithResponseHeader(null!, "value"));
        Assert.ThrowsAny<ArgumentException>(() => app.MapGet("/null-value", () => TypedResults.Ok()).WithResponseHeader("X-Test", null!));
        Assert.ThrowsAny<ArgumentException>(() => app.MapGet("/empty-values", () => TypedResults.Ok()).WithResponseHeader(new ResponseHeaderAttribute("X-Test", Array.Empty<string>())));
        Assert.ThrowsAny<ArgumentException>(() => app.MapGet("/null-values", () => TypedResults.Ok()).WithResponseHeader(new ResponseHeaderAttribute("X-Test", (string[])null!)));
        Assert.ThrowsAny<ArgumentException>(() => app.MapGet("/invalid-type", () => TypedResults.Ok()).WithResponseHeader("X-Test", "value", typeof(string)));
    }

    [Fact]
    public async Task Disabled_namespaces_do_not_construct_invalid_policies_or_map_endpoints()
    {
        await using var app = CreateHost();
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/disabled");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(Endpoints(app), endpoint => endpoint.RoutePattern.RawText?.Contains("disabled", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Hosts_keep_separate_header_policies_and_custom_wire_names()
    {
        await using var first = CreateHost();
        await using var second = CreateHost();
        first.MapGet("/host", () => TypedResults.Ok()).WithResponseHeader("X-Custom-Host", "first");
        second.MapGet("/host", () => TypedResults.Ok()).WithResponseHeader("X-Custom-Host", "second");
        await first.StartAsync();
        await second.StartAsync();
        using var firstClient = first.GetTestClient();
        using var secondClient = second.GetTestClient();
        using var firstResponse = await firstClient.GetAsync("/host");
        using var secondResponse = await secondClient.GetAsync("/host");
        Assert.Equal("first", Header(firstResponse, "X-Custom-Host"));
        Assert.Equal("second", Header(secondResponse, "X-Custom-Host"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Native_group_headers_observe_their_registration_position_around_a_short_circuit(bool headerFirst)
    {
        await using var app = CreateHost();
        var group = app.MapGroup("/ordered-policy");
        if (headerFirst)
        {
            group.WithResponseHeader("X-Order", "before guard");
        }

        group.AddEndpointFilter((context, next) => ValueTask.FromResult<object?>(TypedResults.Conflict("guard short circuit")));
        if (!headerFirst)
        {
            group.WithResponseHeader("X-Order", "after guard");
        }

        group.MapGet("/value", () => TypedResults.Ok("unreachable"));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/ordered-policy/value");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(headerFirst, response.Headers.Contains("X-Order"));
        if (headerFirst)
        {
            Assert.Equal("before guard", Header(response, "X-Order"));
        }
    }

    [Fact]
    public async Task Changing_a_retained_attribute_result_selector_does_not_change_an_existing_registration()
    {
        await using var app = CreateHost();
        var declaration = new ResponseHeaderAttribute("X-Snapshot", "original") { ResultType = typeof(Ok<>) };
        app.MapGet("/selector-snapshot", () => TypedResults.Ok("matching result")).WithResponseHeader(declaration);
        declaration.ResultType = typeof(NotFound);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/selector-snapshot");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("original", Header(response, "X-Snapshot"));
    }

    [Fact]
    public async Task A_result_selected_header_does_not_mutate_a_response_started_by_the_handler()
    {
        await using var app = CreateHost();
        app.MapGet("/already-started", async (HttpContext context) =>
        {
            await context.Response.WriteAsync("body already written");
            return TypedResults.Empty;
        }).WithResponseHeader("X-Late", "must not be added", typeof(EmptyHttpResult));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/already-started");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("body already written", await response.Content.ReadAsStringAsync());
        Assert.False(response.Headers.Contains("X-Late"));
    }

    [Fact]
    public async Task Cyclic_custom_nested_results_finish_without_matching_an_unrelated_selector()
    {
        await using var app = CreateHost();
        app.MapGet("/cyclic-result", () => new CyclicResult()).WithResponseHeader("X-Cycle", "must not match", typeof(Ok<>));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/cyclic-result").WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("cyclic custom result", await response.Content.ReadAsStringAsync());
        Assert.False(response.Headers.Contains("X-Cycle"));
    }

    private static WebApplication CreateHost() => ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
    private static string Header(HttpResponseMessage response, string name) => Assert.Single(response.Headers.GetValues(name));
    private static IEnumerable<RouteEndpoint> Endpoints(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    private static RouteEndpoint Endpoint(WebApplication app, string pattern) => Assert.Single(Endpoints(app), endpoint => endpoint.RoutePattern.RawText == pattern);
    private static string Describe(IProducesResponseTypeMetadata metadata) => metadata.StatusCode + ":" + metadata.Type?.FullName + ":" + string.Join(",", metadata.ContentTypes);

    private sealed class CyclicResult : IResult, INestedHttpResult
    {
        public IResult Result => this;
        public Task ExecuteAsync(HttpContext httpContext) => httpContext.Response.WriteAsync("cyclic custom result");
    }
}