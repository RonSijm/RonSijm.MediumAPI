using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.ResponseFallbackExample.Endpoints.Inheritance;
using Xunit;
using static RonSijm.MediumAPI.ResponseFallbackExample.IntegrationTest.FallbackHost;

namespace RonSijm.MediumAPI.ResponseFallbackExample.IntegrationTest;

public sealed class NativeResponseFallbackTests
{
    [Fact]
    public void Marker_reuses_response_defaults_and_immutable_values_without_itself_declaring_a_response()
    {
        Assert.False(typeof(IProducesResponseTypeMetadata).IsAssignableFrom(typeof(EndpointResponseFallbackAttribute)));
        foreach (var name in new[] { nameof(EndpointResponseFallbackAttribute.StatusCode), nameof(EndpointResponseFallbackAttribute.Type), nameof(EndpointResponseFallbackAttribute.ContentTypes) })
        {
            Assert.Null(typeof(EndpointResponseFallbackAttribute).GetProperty(name)!.SetMethod);
        }

        var statusOnly = new EndpointResponseFallbackAttribute(404);
        Assert.Equal(404, statusOnly.StatusCode);
        Assert.Equal(typeof(void), statusOnly.Type);
        Assert.Empty(statusOnly.ContentTypes);
        var extra = new[] { "text/plain", "application/json" };
        var fallback = new EndpointResponseFallbackAttribute(422, typeof(FirstFailure), null, extra);
        extra[0] = "application/xml";
        Assert.Equal(typeof(FirstFailure), fallback.Type);
        Assert.Equal(new[] { "application/json", "text/plain", "application/json" }, fallback.ContentTypes);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)fallback.ContentTypes)[0] = "application/xml");
        Assert.Throws<ArgumentNullException>(() => new EndpointResponseFallbackAttribute(404, typeof(FirstFailure), "text/plain", null!));
    }

    [Fact]
    public async Task Ordered_response_inventory_route_contracts_and_real_API_Explorer_match_independent_native_callbacks()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        await app.StartAsync();
        await native.StartAsync();
        Assert.Equal(20, Routes(app).Count());
        Assert.Equal(Inventory(native), Inventory(app));
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
        var descriptions = Descriptions(app);
        Assert.Equal(20, descriptions.Operations.Count);
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), descriptions));
        Assert.Equal(0, Probe(app).TargetActivations);
        Assert.Equal(0, Probe(app).TotalCalls);
        Assert.Equal(0, Probe(app).FilterCalls);
        Assert.Equal(0, Probe(native).TotalCalls);
    }

    [Fact]
    public async Task Any_existing_status_preserves_explicit_problem_typed_empty_and_custom_provider_contracts()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var problem = Assert.Single(Responses(app, "/fallback/guarded/problem", 404));
            Assert.Equal(typeof(ProblemDetails), problem.Type);
            Assert.Equal(new[] { "application/problem+json" }, problem.ContentTypes);
            var typed = Assert.Single(Responses(app, "/fallback/guarded/typed", 404));
            Assert.Equal(typeof(void), typed.Type);
            Assert.Empty(typed.ContentTypes);
            var provider = Assert.Single(Responses(app, "/fallback/guarded/provider", 404));
            Assert.IsType<ProviderResponseMetadata>(provider);
            Assert.Equal(typeof(ProviderFailure), provider.Type);
            Assert.Equal(new[] { "application/x-provider-error" }, provider.ContentTypes);
        }
    }

    [Fact]
    public async Task Missing_status_is_added_from_filter_endpoint_class_and_original_handler_declarations()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            foreach (var path in new[] { "/fallback/guarded/plain", "/fallback/class-default", "/fallback/method-default" })
            {
                var fallback = Assert.Single(Responses(app, path, 404));
                Assert.Equal(typeof(void), fallback.Type);
                Assert.Empty(fallback.ContentTypes);
                var success = Assert.Single(Responses(app, path, 200));
                Assert.Equal(typeof(FallbackReply), success.Type);
            }

            var explicitClass = Assert.Single(Responses(app, "/fallback/class-explicit", 404));
            Assert.Equal(typeof(ProblemDetails), explicitClass.Type);
            Assert.Equal(new[] { "application/problem+json" }, explicitClass.ContentTypes);
        }
    }

    [Fact]
    public async Task A_filters_own_explicit_response_precedes_its_fallback_and_is_never_replaced_or_merged()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var response = Assert.Single(Responses(app, "/fallback/filter-explicit", 404));
            Assert.Equal(typeof(FilterFailure), response.Type);
            Assert.Equal(new[] { "application/json" }, response.ContentTypes);
        }
    }

    [Fact]
    public async Task Namespace_and_group_filter_finals_see_typed_descendant_metadata_before_supplying_defaults()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var plain = Assert.Single(Responses(app, "/fallback/grouped/plain", 404));
            Assert.Equal(typeof(NamespaceFailure), plain.Type);
            Assert.Equal(new[] { "application/json" }, plain.ContentTypes);
            foreach (var path in new[] { "/fallback/grouped/typed", "/fallback/group-filter/plain", "/fallback/group-filter/typed" })
            {
                var response = Assert.Single(Responses(app, path, 404));
                Assert.Equal(typeof(void), response.Type);
                Assert.Empty(response.ContentTypes);
            }
        }
    }

    [Fact]
    public async Task Native_endpoint_child_parent_and_later_sibling_final_order_is_preserved_without_global_deduplication()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var child = Assert.Single(Responses(app, "/fallback/final/child/plain", 404));
            Assert.Equal(typeof(ChildFailure), child.Type);
            var explicitFinal = Assert.Single(Responses(app, "/fallback/final/child/explicit", 404));
            Assert.Equal(typeof(FinalFailure), explicitFinal.Type);
            Assert.Equal(new[] { "application/x-final-error" }, explicitFinal.ContentTypes);
            var later = Responses(app, "/fallback/later-final/value", 404);
            Assert.Equal(2, later.Length);
            Assert.Equal(typeof(FirstFailure), later[0].Type);
            Assert.Equal(new[] { "application/json" }, later[0].ContentTypes);
            Assert.Equal(typeof(LaterFailure), later[1].Type);
            Assert.Equal(new[] { "text/plain" }, later[1].ContentTypes);
        }
    }

    [Fact]
    public async Task Repeated_class_and_method_fallbacks_keep_the_first_present_status_without_merging_schemas_or_media()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var response = Assert.Single(Responses(app, "/fallback/repeated", 404));
            Assert.Equal(typeof(FirstFailure), response.Type);
            Assert.Equal(new[] { "application/json" }, response.ContentTypes);
        }
    }

    [Fact]
    public async Task Class_group_and_filter_lookup_is_direct_only_while_inherited_original_handler_fallbacks_remain()
    {
        await using var app = Create();
        await app.StartAsync();
        var route = Route(app, "/fallback/inheritance/value");
        var responses = route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        Assert.Equal(new[] { 200, 422, 403, 409, 503 }, responses.Select(response => response.StatusCode));
        Assert.Equal(typeof(InheritedEndpointBase), route.Metadata.GetMetadata<MethodInfo>()?.DeclaringType);
        Assert.Equal(typeof(GetInheritedEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType);
        Assert.DoesNotContain(responses, response => response.StatusCode is 400 or 401 or 410 or 429);
        Assert.Equal(0, Probe(app).TargetActivations);
    }

    [Fact]
    public async Task Explicit_aliases_manual_patterns_and_disabled_groups_keep_their_existing_discovery_boundaries()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            foreach (var path in new[] { "/aliases/first", "/aliases/second" })
            {
                Assert.Single(Responses(app, path, 410));
                Assert.Empty(Responses(app, path, 404));
                Assert.Null(Route(app, path).Metadata.GetMetadata<ITagsMetadata>());
            }

            using var client = app.GetTestClient();
            foreach (var path in new[] { "/unmapped-alias", "/fallback/disabled", "/value" })
            {
                using var response = await client.GetAsync(path);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }

            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }

    [Theory]
    [InlineData("/fallback/guarded/plain", "guarded")]
    [InlineData("/fallback/class-default", "class-default")]
    [InlineData("/fallback/class-explicit", "class-explicit")]
    [InlineData("/fallback/method-default", "method-default")]
    [InlineData("/fallback/repeated", "repeated")]
    [InlineData("/fallback/grouped/plain", "grouped-plain")]
    [InlineData("/fallback/group-filter/plain", "group-filter-plain")]
    [InlineData("/fallback/final/child/plain", "child-plain")]
    [InlineData("/fallback/final/child/explicit", "child-explicit")]
    [InlineData("/fallback/inheritance/value", "inherited")]
    [InlineData("/fallback/later-final/value", "later-final")]
    [InlineData("/aliases/first", "alias")]
    [InlineData("/aliases/second", "alias")]
    public async Task Fallback_documentation_never_changes_a_successful_HTTP_payload_status_or_media_type(string path, string source)
    {
        string? expectedBody = null;
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync();
            if (native)
            {
                expectedBody = body;
            }
            else
            {
                Assert.Equal(expectedBody, body);
            }

            Assert.Equal(new FallbackReply(source, "available"), await response.Content.ReadFromJsonAsync<FallbackReply>());
            Assert.Equal(1, Probe(app).TotalCalls);
        }
    }

    [Theory]
    [InlineData("/fallback/guarded/plain")]
    [InlineData("/fallback/guarded/problem")]
    [InlineData("/fallback/guarded/typed")]
    [InlineData("/fallback/guarded/provider")]
    [InlineData("/fallback/group-filter/plain")]
    [InlineData("/fallback/group-filter/typed")]
    public async Task Native_filter_short_circuits_keep_their_empty_404_without_calling_the_handler(string path)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path + "?missing=true");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.Null(response.Content.Headers.ContentType);
            Assert.Equal(1, Probe(app).FilterCalls);
            Assert.Equal(0, Probe(app).TotalCalls);
            if (path == "/fallback/guarded/plain")
            {
                // The original instance-handler adapter activates its target before native filters; the static native reference has no target.
                Assert.Equal(native ? 0 : 1, Probe(app).TargetActivations);
            }
        }
    }

    [Theory]
    [InlineData("/fallback/guarded/typed")]
    [InlineData("/fallback/grouped/typed")]
    [InlineData("/fallback/group-filter/typed")]
    public async Task Concrete_typed_404_results_remain_empty_and_are_not_wrapped_by_fallback_metadata(string path)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path + "?absent=true");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.Null(response.Content.Headers.ContentType);
            Assert.Equal(1, Probe(app).TotalCalls);
            Assert.Single(Responses(app, path, 404));
        }
    }

    [Fact]
    public async Task Explicit_problem_404_preserves_its_real_body_and_problem_media_type()
    {
        string? expectedBody = null;
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/fallback/guarded/problem?problem=true");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync();
            if (native)
            {
                expectedBody = body;
            }
            else
            {
                Assert.Equal(expectedBody, body);
            }

            using var document = JsonDocument.Parse(body);
            Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("missing-item", document.RootElement.GetProperty("code").GetString());
            Assert.Equal("urn:sample:missing-item", document.RootElement.GetProperty("type").GetString());
            Assert.Equal(1, Probe(app).TotalCalls);
        }
    }

    [Fact]
    public async Task A_filters_own_explicit_404_body_is_returned_without_an_added_fallback_representation()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/fallback/filter-explicit?missing=true");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(new FilterFailure("filtered-item"), await response.Content.ReadFromJsonAsync<FilterFailure>());
            Assert.Equal(0, Probe(app).TotalCalls);
            Assert.Equal(1, Probe(app).FilterCalls);
        }
    }

    [Fact]
    public async Task Dynamic_IResult_outcomes_are_not_inferred_validated_or_exhaustively_documented_by_a_fallback()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            Assert.Single(Responses(app, "/fallback/dynamic", 200));
            Assert.Empty(Responses(app, "/fallback/dynamic", 404));
            using var client = app.GetTestClient();
            using var missing = await client.GetAsync("/fallback/dynamic?absent=true");
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.Equal("", await missing.Content.ReadAsStringAsync());
            using var present = await client.GetAsync("/fallback/dynamic");
            Assert.Equal(HttpStatusCode.OK, present.StatusCode);
            Assert.Equal(new FallbackReply("dynamic", "available"), await present.Content.ReadFromJsonAsync<FallbackReply>());
            Assert.Equal(2, Probe(app).TotalCalls);
        }
    }

    [Fact]
    public async Task Custom_provider_metadata_does_not_force_its_documented_status_or_payload_onto_the_HTTP_result()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/fallback/guarded/provider");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.Equal(1, Probe(app).TotalCalls);
            Assert.IsType<ProviderResponseMetadata>(Assert.Single(Responses(app, "/fallback/guarded/provider", 404)));
        }
    }

    [Fact]
    public async Task Native_query_binding_failures_remain_a_400_while_native_filters_still_run()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/fallback/guarded/plain?missing=invalid");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            // Native TryParse failures still enter filters; the terminal delegate skips the handler.
            Assert.Equal(1, Probe(app).FilterCalls);
            Assert.Equal(0, Probe(app).TotalCalls);
            Assert.Equal(native ? 0 : 1, Probe(app).TargetActivations);
        }
    }

    [Fact]
    public async Task Independent_hosts_get_distinct_fallback_metadata_without_startup_endpoint_activation()
    {
        await using var first = Create();
        await using var second = Create();
        await first.StartAsync();
        await second.StartAsync();
        var firstResponse = Assert.Single(Responses(first, "/fallback/grouped/plain", 404));
        var secondResponse = Assert.Single(Responses(second, "/fallback/grouped/plain", 404));
        Assert.NotSame(firstResponse, secondResponse);
        Assert.Equal(Value(firstResponse), Value(secondResponse));
        Assert.Equal(0, Probe(first).TargetActivations);
        Assert.Equal(0, Probe(second).TargetActivations);
        Assert.Equal(0, Probe(first).TotalCalls);
        Assert.Equal(0, Probe(second).TotalCalls);
    }
}