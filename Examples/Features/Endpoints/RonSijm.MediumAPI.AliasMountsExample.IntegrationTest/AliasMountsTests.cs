using System.Collections;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.AliasMountsExample.Endpoints.Owned;
using Xunit;
using static RonSijm.MediumAPI.AliasMountsExample.IntegrationTest.AliasHost;

namespace RonSijm.MediumAPI.AliasMountsExample.IntegrationTest;

public sealed class AliasMountsTests
{
    [Fact]
    public async Task Full_route_metadata_and_real_API_descriptions_match_legacy_single_maps_with_a_native_aggregate()
    {
        await using var app = Create();
        await using var legacy = Create(legacy: true);
        await app.StartAsync();
        await legacy.StartAsync();
        Assert.Equal(9, Routes(app).Count());
        Equivalent(MediumApiContractComparison.Compare(legacy.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
        var actual = Descriptions(app);
        var expected = Descriptions(legacy);
        Assert.Equal(9, actual.Operations.Count);
        Assert.Equal(9, expected.Operations.Count);
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(expected, actual));
        Assert.Equal(0, Probe(app).Activations);
        Assert.Equal(0, Probe(app).Calls);
    }

    [Fact]
    public async Task Builder_collection_and_all_null_entries_are_validated_before_any_registration_lookup_or_mapping()
    {
        await using var app = Create(map: false);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => MediumApiEndpointAliasExtensions.MapMediumApiEndpointAliases<IGetEndpoint>(null!, [])).ParamName);
        Assert.Equal("patterns", Assert.Throws<ArgumentNullException>(() => app.MapMediumApiEndpointAliases<IGetEndpoint>(null!)).ParamName);
        Assert.Equal("patterns", Assert.Throws<ArgumentException>(() => app.MapMediumApiEndpointAliases<IGetEndpoint>(["/first", null!, "/last"])).ParamName);
        Assert.Empty(Routes(app));
    }

    [Fact]
    public async Task Empty_input_returns_the_native_noop_aggregate_without_requiring_a_registered_endpoint_type()
    {
        await using var app = Create(map: false);
        var aggregate = app.MapMediumApiEndpointAliases<IGetEndpoint>([]);
        Assert.IsType<RouteHandlerBuilder>(aggregate);
        var calls = 0;
        aggregate.Add(_ => calls++);
        ((IEndpointConventionBuilder)aggregate).Finally(_ => calls++);
        Assert.Empty(Routes(app));
        Assert.Equal(0, calls);
        Assert.Equal(0, Probe(app).Activations);
    }

    [Fact]
    public async Task A_one_shot_sequence_is_snapshotted_once_and_later_native_conventions_do_not_remap_aliases()
    {
        await using var app = Create(map: false);
        var input = new OneShotPatterns(["/first/{id:int}", "/second/{id:int}"]);
        var aggregate = app.MapMediumApiEndpointAliases<SharedAliasEndpoint>(input);
        Assert.IsType<RouteHandlerBuilder>(aggregate);
        aggregate.Add(builder => builder.Metadata.Add(new CallerConvention("ordinary")));
        ((IEndpointConventionBuilder)aggregate).Finally(builder => builder.Metadata.Add(new CallerConvention("final")));
        var routes = Routes(app).ToArray();
        Assert.Equal(2, routes.Length);
        Assert.All(routes, route => Assert.Equal(new[] { "ordinary", "final" }, route.Metadata.GetOrderedMetadata<CallerConvention>().Select(value => value.Phase)));
        Assert.Equal(1, input.Enumerations);
    }

    [Fact]
    public async Task Later_mutation_of_the_callers_pattern_array_cannot_change_or_add_mapped_aliases()
    {
        await using var app = Create(map: false);
        var input = new[] { "/first/{id:int}", "/second/{id:int}" };
        app.MapMediumApiEndpointAliases<SharedAliasEndpoint>(input);
        input[0] = "/mutated/{id:int}";
        Assert.Equal(new[] { "/first/{id:int}", "/second/{id:int}" }, Routes(app).Select(route => route.RoutePattern.RawText));
    }

    [Fact]
    public async Task A_sequence_failure_during_snapshotting_propagates_before_any_endpoint_is_added()
    {
        await using var app = Create(map: false);
        var failure = new InvalidOperationException("Synthetic sequence failure.");

        IEnumerable<string> Broken()
        {
            yield return "/first/{id:int}";
            throw failure;
        }

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => app.MapMediumApiEndpointAliases<SharedAliasEndpoint>(Broken())));
        Assert.Empty(Routes(app));
    }

    [Fact]
    public async Task Exact_pattern_strings_input_order_empty_patterns_and_duplicates_are_retained()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(map: false);
            var input = new[] { "/Exact/", "/Exact/", "" };
            if (legacy)
            {
                LegacyAliases<NamedAliasEndpoint>(app, input);
            }
            else
            {
                app.MapMediumApiEndpointAliases<NamedAliasEndpoint>(input);
            }

            Assert.Equal(input, Routes(app).Select(route => route.RoutePattern.RawText));
        }
    }

    [Fact]
    public async Task A_later_native_route_pattern_failure_keeps_earlier_mappings_without_a_transaction_or_new_error_policy()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(map: false);
            var input = new[] { "/before/{id:int}", "/{broken" };
            if (legacy)
            {
                Assert.Throws<RoutePatternException>(() => LegacyAliases<SharedAliasEndpoint>(app, input));
            }
            else
            {
                Assert.Throws<RoutePatternException>(() => app.MapMediumApiEndpointAliases<SharedAliasEndpoint>(input));
            }

            Assert.Equal("/before/{id:int}", Assert.Single(Routes(app)).RoutePattern.RawText);
        }
    }

    [Fact]
    public async Task Suppression_affects_only_the_aliases_fallback_names_and_leaves_other_mappings_unchanged()
    {
        await using var app = Create(map: false);
        app.MapMediumApiEndpointAliases<SharedAliasEndpoint>(["/first/{id:int}", "/second/{id:int}"]);
        app.MapMediumApiEndpoint<AutomaticEndpoint>();
        Assert.Null(Route(app, "/first/{id:int}").Metadata.GetMetadata<IEndpointNameMetadata>());
        Assert.Null(Route(app, "/second/{id:int}").Metadata.GetMetadata<IRouteNameMetadata>());
        Assert.Equal("Automatic", Route(app, "/automatic").Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
    }

    [Fact]
    public async Task An_established_explicit_name_survives_automatic_fallback_suppression()
    {
        await using var app = Create();
        var route = Route(app, "/named/one");
        Assert.Equal("ExplicitAlias", route.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
        await app.StartAsync();
        Assert.Equal("/named/one", app.Services.GetRequiredService<LinkGenerator>().GetPathByName("ExplicitAlias", values: null));
    }

    [Fact]
    public async Task Repeated_explicit_names_keep_the_same_native_collision_error_instead_of_being_renamed()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(map: false);
            if (legacy)
            {
                LegacyAliases<NamedAliasEndpoint>(app, ["/bad/first", "/bad/second"]);
            }
            else
            {
                app.MapMediumApiEndpointAliases<NamedAliasEndpoint>(["/bad/first", "/bad/second"]);
            }

            var failure = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await app.StartAsync();
                using var client = app.GetTestClient();
                using var response = await client.GetAsync("/bad/first");
            });
            Assert.Contains("ExplicitAlias", failure.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Aggregate_filter_Configure_shares_one_metadata_object_while_endpoint_Configure_remains_per_alias()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            var first = Route(app, "/root/first/{id:int}");
            var second = Route(app, "/root/second/{id:int}");
            var shared = Assert.Single(first.Metadata.GetOrderedMetadata<AggregateConfigurationMetadata>());
            Assert.Same(shared, Assert.Single(second.Metadata.GetOrderedMetadata<AggregateConfigurationMetadata>()));
            shared.Touches = 7;
            Assert.Equal(7, second.Metadata.GetMetadata<AggregateConfigurationMetadata>()!.Touches);
            Assert.NotSame(Assert.Single(first.Metadata.GetOrderedMetadata<PerMappingConfigurationMetadata>()), Assert.Single(second.Metadata.GetOrderedMetadata<PerMappingConfigurationMetadata>()));
        }
    }

    [Fact]
    public async Task Alias_mapping_keeps_declared_namespace_ownership_manual_exclusion_and_ordinary_automatic_discovery_separate()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var automatic = await client.GetAsync("/owned/automatic");
            Assert.Equal(HttpStatusCode.OK, automatic.StatusCode);
            Assert.Equal("automatic", Assert.Single(automatic.Headers.GetValues("X-Owned")));
            foreach (var path in new[] { "/declared/7", "/owned/declared/7", "/named-declared" })
            {
                using var missing = await client.GetAsync(path);
                Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            }
        }
    }

    [Fact]
    public async Task Supplied_nested_group_authorization_rejects_anonymous_requests_before_alias_filters_or_activation()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/api/private/first/7");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.False(response.Headers.Contains("X-Group"));
            Assert.False(response.Headers.Contains("X-Aggregate"));
            Assert.False(response.Headers.Contains("X-Filter"));
            Assert.Equal(0, Probe(app).Activations);
            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Aggregate_filters_keep_native_short_circuit_order_without_applying_uncalled_handler_policies()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/public/first/7?stop");
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("public", Assert.Single(response.Headers.GetValues("X-Group")));
            Assert.Equal("Public", Assert.Single(response.Headers.GetValues("X-Aggregate")));
            Assert.Equal("aggregate", Assert.Single(response.Headers.GetValues("X-Filter")));
            Assert.False(response.Headers.Contains("X-Handler"));
            Assert.False(response.Headers.Contains("X-Endpoint"));
            Assert.Equal(0, Probe(app).Calls);
            await Probe(app).FirstDisposal.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(1, Probe(app).Activations);
            Assert.Equal(1, Probe(app).Disposals);
        }
    }

    [Fact]
    public async Task Concrete_typed_missing_results_keep_their_empty_body_metadata_and_handler_headers()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/public/first/0");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.Equal("handler", Assert.Single(response.Headers.GetValues("X-Handler")));
            Assert.Equal("endpoint", Assert.Single(response.Headers.GetValues("X-Endpoint")));
            Assert.Equal(typeof(void), Assert.Single(Route(app, "/public/first/{id:int}").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), value => value.StatusCode == 404).Type);
        }
    }

    [Fact]
    public async Task A_nonempty_unknown_endpoint_still_uses_the_existing_descriptor_lookup_failure()
    {
        await using var app = Create(map: false);
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiEndpointAliases<IGetEndpoint>(["/missing"]));
        Assert.Empty(Routes(app));
    }

    [Fact]
    public async Task Each_alias_retains_its_concrete_target_identity_original_inherited_method_and_typed_responses()
    {
        await using var app = Create();
        var aliases = Routes(app).Where(route => route.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType == typeof(SharedAliasEndpoint)).ToArray();
        Assert.Equal(6, aliases.Length);
        foreach (var route in aliases)
        {
            Assert.Equal(typeof(AliasHandlerBase), route.Metadata.GetMetadata<MethodInfo>()?.DeclaringType);
            Assert.Equal(typeof(SharedAliasEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType);
            Assert.Equal(typeof(AliasReply), Assert.Single(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), value => value.StatusCode == 200).Type);
            Assert.Single(route.Metadata.GetOrderedMetadata<AliasFinalMetadata>());
        }

        Assert.Equal(0, Probe(app).Activations);
    }

    [Theory]
    [InlineData("/root/first/7", "Root", null, "RootFirst", false)]
    [InlineData("/root/second/7", "Root", null, "RootSecond", false)]
    [InlineData("/public/first/7", "Public", "public", "PublicFirst", false)]
    [InlineData("/public/second/7", "Public", "public", "PublicSecond", false)]
    [InlineData("/api/private/first/7", "Private", "private", "PrivateFirst", true)]
    [InlineData("/api/private/second/7", "Private", "private", "PrivateSecond", true)]
    public async Task Alias_HTTP_payloads_DI_headers_final_names_and_native_links_match_the_existing_foreach_mapping(string path, string scope, string? group, string name, bool authenticate)
    {
        string? expectedBody = null;
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            if (authenticate)
            {
                await Authenticate(client);
            }

            using var response = await client.GetAsync(path + "?note=hello");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("endpoint", Assert.Single(response.Headers.GetValues("X-Endpoint")));
            Assert.Equal("handler", Assert.Single(response.Headers.GetValues("X-Handler")));
            Assert.Equal(scope, Assert.Single(response.Headers.GetValues("X-Aggregate")));
            Assert.Equal("aggregate", Assert.Single(response.Headers.GetValues("X-Filter")));
            Assert.False(response.Headers.Contains("X-Owned"));
            if (group is not null)
            {
                Assert.Equal(group, Assert.Single(response.Headers.GetValues("X-Group")));
            }
            else
            {
                Assert.False(response.Headers.Contains("X-Group"));
            }

            Assert.Equal(new AliasReply(7, "hello", path), await response.Content.ReadFromJsonAsync<AliasReply>());
            var body = await response.Content.ReadAsStringAsync();
            if (legacy)
            {
                expectedBody = body;
            }
            else
            {
                Assert.Equal(expectedBody, body);
            }

            Assert.Equal(path, app.Services.GetRequiredService<LinkGenerator>().GetPathByName(name, new { id = 7 }));
            await Probe(app).FirstDisposal.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(1, Probe(app).Activations);
            Assert.Equal(1, Probe(app).Calls);
            Assert.Equal(1, Probe(app).Disposals);
        }
    }

    private sealed record CallerConvention(string Phase);

    private sealed class OneShotPatterns : IEnumerable<string>
    {
        private readonly string[] patterns;
        public int Enumerations { get; private set; }

        public OneShotPatterns(string[] patterns)
        {
            this.patterns = patterns;
        }

        public IEnumerator<string> GetEnumerator()
        {
            Enumerations++;
            if (Enumerations != 1)
            {
                throw new InvalidOperationException("The sequence cannot be enumerated twice.");
            }

            return ((IEnumerable<string>)patterns).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}