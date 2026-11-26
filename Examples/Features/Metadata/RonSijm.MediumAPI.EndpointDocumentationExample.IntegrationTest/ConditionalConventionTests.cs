using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Xunit;
using static RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.EndpointDocumentationHost;
using ExampleProgram = RonSijm.MediumAPI.EndpointDocumentationExample.Program;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest;

public sealed class ConditionalConventionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Deferred_native_response_metadata_preserves_exact_order_API_Explorer_and_complete_Swagger_documents(bool? json)
    {
        await using var native = ProfileHost(false, json);
        await using var actual = ProfileHost(true, json);
        await native.StartAsync();
        await actual.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), actual.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(actual)));
        foreach (var document in new[] { DocumentationGroups.Public, DocumentationGroups.Admin })
        {
            Equivalent(MediumApiContractComparison.CompareOpenApi(ExampleProgram.GetOpenApiJson(native.Services, document), ExampleProgram.GetOpenApiJson(actual.Services, document)));
        }

        Assert.Equal(ResponseShapes(Route(native, "/conditional/value")), ResponseShapes(Route(actual, "/conditional/value")));
        var responses = Route(actual, "/conditional/value").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        if (json == true)
        {
            Assert.Equal(new[] { 200, 400, 302, 418, 400 }, responses.TakeLast(5).Select(response => response.StatusCode));
            Assert.Equal(typeof(void), responses[^3].Type);
            Assert.Null(responses[^2].Type);
            Assert.Empty(responses[^3].ContentTypes);
            Assert.Empty(responses[^2].ContentTypes);
        }
        else { Assert.DoesNotContain(responses, response => response.StatusCode is 302 or 418); }

        using var nativeClient = native.GetTestClient();
        using var actualClient = actual.GetTestClient();
        using var expected = await nativeClient.GetAsync("/conditional/value");
        using var response = await actualClient.GetAsync("/conditional/value");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected.StatusCode, response.StatusCode);
        Assert.Equal(expected.Content.Headers.ContentType?.ToString(), response.Content.Headers.ContentType?.ToString());
        Assert.Equal(await expected.Content.ReadAsByteArrayAsync(), await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task False_conditions_do_not_invoke_a_throwing_callback_or_remove_existing_native_metadata()
    {
        await using var app = EmptyHost();
        var endpoint = app.MapGet("/false", (Delegate)(Func<Ok<ConditionalReply>>)Reply).WithName("FalseCondition");
        var existing = new ConditionalStage("existing");
        endpoint.WithMetadata(existing);
        var calls = 0;
        endpoint.AddConditionalConvention(_ => false, _ =>
        {
            calls++;
            throw new InvalidOperationException("An unselected callback must not run.");
        });
        Assert.Equal(0, calls);
        await app.StartAsync();
        Assert.Same(existing, Assert.Single(Route(app, "/false").Metadata.GetOrderedMetadata<ConditionalStage>()));
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/false");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Ordinary_Add_observes_prior_last_metadata_without_becoming_a_late_Finally_callback(bool initial, bool later)
    {
        string[]? expected = null;
        foreach (var helper in new[] { false, true })
        {
            await using var app = EmptyHost();
            var events = new List<string>();
            var endpoint = app.MapGet("/order", (Delegate)(Func<Ok<ConditionalReply>>)Reply);
            endpoint.WithMetadata(new SamplePresentation(!initial), new SamplePresentation(initial));
            endpoint.Add(_ => events.Add("before"));
            Register(endpoint, helper, builder =>
            {
                events.Add("condition");
                return builder.Metadata.OfType<SamplePresentation>().Last().UsesJsonResponses;
            }, builder =>
            {
                events.Add("callback");
                builder.Metadata.Add(new ConditionalStage("selected"));
            });
            endpoint.WithMetadata(new SamplePresentation(later));
            endpoint.Add(_ => events.Add("after"));
            endpoint.Finally(builder =>
            {
                events.Add("finally");
                builder.Metadata.Add(new SamplePresentation(!later));
            });
            Assert.Empty(events);
            var route = Route(app, "/order");
            Assert.Equal(initial ? new[] { "before", "condition", "callback", "after", "finally" } : new[] { "before", "condition", "after", "finally" }, events);
            Assert.Equal(initial ? new[] { "selected" } : Array.Empty<string>(), route.Metadata.GetOrderedMetadata<ConditionalStage>().Select(stage => stage.Value));
            Assert.Equal(!later, route.Metadata.GetMetadata<SamplePresentation>()!.UsesJsonResponses);
            if (expected is null) { expected = events.ToArray(); }
            else { Assert.Equal(expected, events); }
        }
    }

    [Fact]
    public async Task Endpoint_group_and_interface_builders_retain_identity_and_callbacks_receive_the_actual_native_endpoint_builder()
    {
        await using var app = EmptyHost();
        var group = app.MapGroup("/identity");
        Assert.Same(group, group.AddConditionalConvention(_ => true, builder => builder.Metadata.Add(new ConditionalStage("group"))));
        var endpoint = group.MapGet("/value", (Delegate)(Func<Ok<ConditionalReply>>)Reply);
        EndpointBuilder? seen = null;
        Assert.Same(endpoint, endpoint.AddConditionalConvention(builder =>
        {
            seen = builder;
            return true;
        }, builder =>
        {
            Assert.Same(seen, builder);
            builder.Metadata.Add(new ConditionalStage("endpoint"));
        }));
        IEndpointConventionBuilder erased = endpoint;
        Assert.Same(erased, erased.AddConditionalConvention(_ => true, builder => builder.DisplayName = "Conditional display"));
        var route = Route(app, "/identity/value");
        Assert.NotNull(seen);
        Assert.Equal(new[] { "group", "endpoint" }, route.Metadata.GetOrderedMetadata<ConditionalStage>().Select(stage => stage.Value));
        Assert.Equal("Conditional display", route.DisplayName);
        Assert.Equal(typeof(ConditionalConventionTests).GetMethod(nameof(Reply), BindingFlags.Static | BindingFlags.NonPublic), route.Metadata.GetMetadata<MethodInfo>());
    }

    [Fact]
    public void All_arguments_are_validated_at_registration_before_any_callback_or_native_Add()
    {
        var builder = new CapturingConventionBuilder();
        var calls = 0;
        Func<EndpointBuilder, bool> condition = _ =>
        {
            calls++;
            return true;
        };
        Action<EndpointBuilder> convention = _ => calls++;
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => ((IEndpointConventionBuilder)null!).AddConditionalConvention(condition, convention)).ParamName);
        Assert.Equal("condition", Assert.Throws<ArgumentNullException>(() => builder.AddConditionalConvention(null!, convention)).ParamName);
        Assert.Equal("convention", Assert.Throws<ArgumentNullException>(() => builder.AddConditionalConvention(condition, null!)).ParamName);
        Assert.Empty(builder.Conventions);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Native_materialization_can_repeat_without_caching_conditions_callbacks_or_metadata_instances()
    {
        foreach (var helper in new[] { false, true })
        {
            var builder = new CapturingConventionBuilder();
            var conditions = 0;
            var callbacks = 0;
            Register(builder, helper, _ =>
            {
                conditions++;
                return conditions % 2 == 1;
            }, endpoint =>
            {
                callbacks++;
                endpoint.Metadata.Add(new ConditionalStage("fresh"));
            });
            Assert.Equal(0, conditions);
            var native = Assert.Single(builder.Conventions);
            var first = Endpoint();
            var second = Endpoint();
            var third = Endpoint();
            native(first);
            native(second);
            native(third);
            Assert.Equal(3, conditions);
            Assert.Equal(2, callbacks);
            Assert.Empty(second.Metadata);
            Assert.NotSame(Assert.Single(first.Metadata), Assert.Single(third.Metadata));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Predicate_and_callback_failures_retain_exact_exception_identity_and_prior_partial_mutations(bool conditionFails)
    {
        var failure = new InvalidOperationException("Fixed synthetic conditional failure.");
        foreach (var helper in new[] { false, true })
        {
            var builder = new CapturingConventionBuilder();
            var endpoint = Endpoint();
            builder.Add(value => value.Metadata.Add(new ConditionalStage("before")));
            Register(builder, helper, _ =>
            {
                if (conditionFails) { throw failure; }

                return true;
            }, value =>
            {
                value.Metadata.Add(new ConditionalStage("partial"));
                throw failure;
            });
            builder.Add(value => value.Metadata.Add(new ConditionalStage("after")));
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => Apply(builder, endpoint)));
            Assert.Equal(conditionFails ? new[] { "before" } : new[] { "before", "partial" }, endpoint.Metadata.OfType<ConditionalStage>().Select(stage => stage.Value));
        }
    }

    [Fact]
    public void Callback_metadata_retains_exact_references_duplicates_nulls_and_append_order_without_a_response_engine()
    {
        var first = new ProducesResponseTypeMetadata(400, typeof(ProblemDetails), new[] { "application/problem+json" });
        var second = new ProducesResponseTypeMetadata(400, typeof(void), Array.Empty<string>());
        foreach (var helper in new[] { false, true })
        {
            var builder = new CapturingConventionBuilder();
            Register(builder, helper, _ => true, value =>
            {
                value.Metadata.Add(first);
                value.Metadata.Add(second);
                value.Metadata.Add(first);
                value.Metadata.Add(null!);
            });
            var endpoint = Endpoint();
            Apply(builder, endpoint);
            Assert.Equal(4, endpoint.Metadata.Count);
            Assert.Same(first, endpoint.Metadata[0]);
            Assert.Same(second, endpoint.Metadata[1]);
            Assert.Same(first, endpoint.Metadata[2]);
            Assert.Null(endpoint.Metadata[3]);
        }
    }

    [Fact]
    public async Task Native_group_Add_and_Finally_order_is_preserved_around_child_endpoint_conventions()
    {
        foreach (var helper in new[] { false, true })
        {
            await using var app = EmptyHost();
            var group = app.MapGroup("/group").WithMetadata(new SamplePresentation(true));
            Register(group, helper, value => value.Metadata.OfType<SamplePresentation>().Last().UsesJsonResponses, value => value.Metadata.Add(new ConditionalStage("group-add")));
            ((IEndpointConventionBuilder)group).Finally(value => value.Metadata.Add(new ConditionalStage("group-finally")));
            var endpoint = group.MapGet("/value", (Delegate)(Func<Ok<ConditionalReply>>)Reply);
            Register(endpoint, helper, _ => true, value => value.Metadata.Add(new ConditionalStage("endpoint-add")));
            endpoint.Finally(value => value.Metadata.Add(new ConditionalStage("endpoint-finally")));
            Assert.Equal(new[] { "group-add", "endpoint-add", "endpoint-finally", "group-finally" }, Route(app, "/group/value").Metadata.GetOrderedMetadata<ConditionalStage>().Select(stage => stage.Value));
        }
    }

    [Fact]
    public async Task Aggregate_aliases_get_fresh_callback_objects_and_another_host_does_not_share_them()
    {
        await using var first = EmptyHost();
        await using var second = EmptyHost();
        foreach (var app in new[] { first, second })
        {
            var aliases = app.MapMediumApiEndpointAliases<ConditionalAliasEndpoint>(new[] { "/conditional-alias/one", "/conditional-alias/two" });
            Assert.Same(aliases, aliases.AddConditionalConvention(_ => true, value => value.Metadata.Add(new ConditionalStage("alias"))));
        }

        var one = Assert.Single(Route(first, "/conditional-alias/one").Metadata.GetOrderedMetadata<ConditionalStage>());
        var two = Assert.Single(Route(first, "/conditional-alias/two").Metadata.GetOrderedMetadata<ConditionalStage>());
        Assert.NotSame(one, two);
        Assert.NotSame(one, Assert.Single(Route(second, "/conditional-alias/one").Metadata.GetOrderedMetadata<ConditionalStage>()));
        Assert.All(Routes(first), value => Assert.Null(value.Metadata.GetMetadata<IEndpointNameMetadata>()));
    }

    [Fact]
    public async Task Native_first_dispatch_can_materialize_the_graph_while_subsequent_requests_do_not_repeat_startup_callbacks()
    {
        foreach (var helper in new[] { false, true })
        {
            await using var app = EmptyHost();
            var conditions = 0;
            var callbacks = 0;
            var endpoint = app.MapGet("/dispatch", (Delegate)(Func<Ok<ConditionalReply>>)Reply);
            Register(endpoint, helper, _ =>
            {
                conditions++;
                return true;
            }, value =>
            {
                callbacks++;
                value.Metadata.Add(new ConditionalStage("metadata-only"));
            });
            await app.StartAsync();
            Assert.Single(Routes(app));
            using var client = app.GetTestClient();
            using (var first = await client.GetAsync("/dispatch")) { Assert.Equal(HttpStatusCode.OK, first.StatusCode); }

            var materializedConditions = conditions;
            var materializedCallbacks = callbacks;
            Assert.True(materializedConditions > 0);
            Assert.Equal(materializedConditions, materializedCallbacks);
            for (var index = 0; index < 2; index++)
            {
                using var response = await client.GetAsync("/dispatch");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            Assert.Equal(materializedConditions, conditions);
            Assert.Equal(materializedCallbacks, callbacks);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Existing_public_startup_convention_mode_runs_the_helper_without_changing_default_discovery(bool json)
    {
        await using var selected = ExampleProgram.Build(new[] { "--Sample:StartupConventions=true", "--Sample:JsonResponses=" + json }, configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        });
        await selected.StartAsync();
        Assert.Equal(7, Routes(selected).Count());
        Assert.Equal(json, Route(selected, "/conventions/authenticate").Metadata.GetMetadata<SamplePresentation>()!.UsesJsonResponses);
        Assert.Equal(json ? 3 : 0, Route(selected, "/conventions/authenticate").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Count);
        using var client = selected.GetTestClient();
        using var response = await client.PostAsync("/conventions/authenticate", null);
        Assert.Equal(json ? HttpStatusCode.OK : HttpStatusCode.Redirect, response.StatusCode);
        await using var defaults = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        });
        await defaults.StartAsync();
        Assert.Equal(8, Routes(defaults).Count());
        Assert.DoesNotContain(Routes(defaults), route => route.RoutePattern.RawText!.StartsWith("/conditional", StringComparison.Ordinal));
    }

    private static WebApplication EmptyHost()
    {
        return ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        }, mapEndpoints: false);
    }

    private static WebApplication ProfileHost(bool helper, bool? json)
    {
        var app = EmptyHost();
        var group = app.MapGroup("/conditional").WithGroupName(DocumentationGroups.Public).WithTags("Conditional conventions");
        if (json.HasValue) { group.WithMetadata(new SamplePresentation(json.Value)); }

        var endpoint = group.MapGet("/value", (Delegate)(Func<Ok<ConditionalReply>>)Reply).WithName("ConditionalValue");
        Register(endpoint, helper, value => value.Metadata.OfType<SamplePresentation>().LastOrDefault()?.UsesJsonResponses == true, AddResponses);
        return app;
    }

    private static void AddResponses(EndpointBuilder endpoint)
    {
        endpoint.Metadata.Add(new ProducesResponseTypeMetadata(200, typeof(ConditionalReply), new[] { "application/json" }));
        endpoint.Metadata.Add(new ProducesResponseTypeMetadata(400, typeof(ProblemDetails), new[] { "application/problem+json" }));
        endpoint.Metadata.Add(new ProducesResponseTypeMetadata(302, typeof(void), Array.Empty<string>()));
        endpoint.Metadata.Add(new ProducesResponseTypeMetadata(418, null, Array.Empty<string>()));
        endpoint.Metadata.Add(new ProducesResponseTypeMetadata(400, typeof(ProblemDetails), new[] { "application/problem+json" }));
    }

    private static void Register(IEndpointConventionBuilder builder, bool helper, Func<EndpointBuilder, bool> condition, Action<EndpointBuilder> convention)
    {
        if (helper) { builder.AddConditionalConvention(condition, convention); }
        else
        {
            builder.Add(endpoint =>
            {
                if (condition(endpoint)) { convention(endpoint); }
            });
        }
    }

    private static RouteEndpointBuilder Endpoint()
    {
        return new RouteEndpointBuilder(_ => Task.CompletedTask, RoutePatternFactory.Parse("/synthetic"), 0);
    }

    private static void Apply(CapturingConventionBuilder builder, EndpointBuilder endpoint)
    {
        foreach (var convention in builder.Conventions) { convention(endpoint); }
    }

    private static string[] ResponseShapes(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(value => value.StatusCode + "|" + value.Type?.AssemblyQualifiedName + "|" + string.Join(",", value.ContentTypes)).ToArray();
    }

    private static Ok<ConditionalReply> Reply()
    {
        return TypedResults.Ok(new ConditionalReply("unchanged"));
    }

    public sealed record ConditionalReply(string Value);

    public sealed record ConditionalStage(string Value);

    private sealed class CapturingConventionBuilder : IEndpointConventionBuilder
    {
        public List<Action<EndpointBuilder>> Conventions { get; } = new();

        public void Add(Action<EndpointBuilder> convention)
        {
            Conventions.Add(convention);
        }
    }
}

public sealed class ConditionalAliasEndpoint : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/conditional-alias";

    public Ok<string> HandleAsync()
    {
        return TypedResults.Ok("alias");
    }
}