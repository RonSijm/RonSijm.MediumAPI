using System.Collections;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using static RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest.ResourcePoliciesHost;

namespace RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest;

public sealed class FilterFactoryPolicyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Early_missing_selection_stays_outside_common_mapping_and_skips_operations(bool nullItems)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var route in StartRoutes())
            {
                using var response = await client.PostAsJsonAsync(route, new GuardedStartRequest(nullItems ? null : Array.Empty<int>(), "coded-validation"));
                Assert.Equal(400, (int)response.StatusCode);
                await AssertProblem(response, "invalid-selection", "Select at least one item.");
                Assert.False(response.Headers.Contains("X-Factory-Scope"));
                Assert.Equal(0, Probe(app).OwnershipChecks);
                Assert.Equal(0, Probe(app).Operations);
            }
        }
    }

    [Theory]
    [InlineData("ok", 200, null)]
    [InlineData("replay", 200, null)]
    [InlineData("coded-validation", 400, "draft-not-ready")]
    [InlineData("conflict", 409, "idempotency-key-reused")]
    [InlineData("late-conflict", 409, "idempotency-key-reused")]
    [InlineData("ownership-missing", 404, null)]
    [InlineData("missing", 404, null)]
    public async Task Later_command_outcomes_keep_typed_contracts_and_resource_order(string outcome, int status, string? code)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var route in StartRoutes())
            {
                var beforeReads = Probe(app).OwnershipChecks;
                var beforeOperations = Probe(app).Operations;
                using var response = await client.PostAsJsonAsync(route, new GuardedStartRequest(new[] { 11, 13 }, outcome));
                Assert.Equal(status, (int)response.StatusCode);
                Assert.Equal(1, Probe(app).OwnershipChecks - beforeReads);
                Assert.Equal(outcome is "ok" or "replay" or "late-conflict" ? 1 : 0, Probe(app).Operations - beforeOperations);
                Assert.True(response.Headers.Contains("X-Factory-Scope"));
                Assert.Equal(outcome == "replay", response.Headers.Contains("Idempotent-Replayed"));
                if (status == 200)
                {
                    var body = await response.Content.ReadFromJsonAsync<GuardedStartedDto>();
                    Assert.Equal(17, body!.Id);
                    Assert.Equal(2, body.SelectedItems);
                    Assert.Equal(body.Scope.ToString(), Assert.Single(response.Headers.GetValues("X-Factory-Scope")));
                }
                else if (code is not null)
                {
                    await AssertProblem(response, code, status == 409 ? "This idempotency key was already used for a different request." : "The request could not be completed.");
                }
                else { Assert.Empty(await response.Content.ReadAsByteArrayAsync()); }
            }
        }
    }

    [Theory]
    [InlineData("/sample/guarded-start")]
    [InlineData("/sample/shifted-start")]
    [InlineData("/sample/start-alias")]
    [InlineData("/sample/factory-namespace/start")]
    public async Task Native_argument_replacement_reaches_the_following_guard_and_original_handler(string route)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(new GuardedStartRequest(null)) };
            request.Headers.Add("X-Replace-Selection", "true");
            using var response = await client.SendAsync(request);
            Assert.Equal(200, (int)response.StatusCode);
            Assert.Equal(2, (await response.Content.ReadFromJsonAsync<GuardedStartedDto>())!.SelectedItems);
            Assert.Equal(1, Probe(app).OwnershipChecks);
            Assert.Equal(1, Probe(app).Operations);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Count_inspection_failures_remain_outside_the_common_policy(bool unexpected)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(false);
            await using var requestScope = app.Services.CreateAsyncScope();
            var context = new DefaultHttpContext { RequestServices = requestScope.ServiceProvider };
            var request = new GuardedStartRequest(new ThrowingItems(unexpected));
            var operations = requestScope.ServiceProvider.GetRequiredService<OperationOutcomeService>();
            var marker = requestScope.ServiceProvider.GetRequiredService<FactoryRequestScope>();

            async Task Invoke()
            {
                if (legacy)
                {
                    await LegacyGuardedStarts.BeforeCommand(request, operations, marker, context, CancellationToken.None);
                    return;
                }

                var factoryContext = new EndpointFilterFactoryContext { MethodInfo = typeof(GuardedStartEndpoint).GetMethod("HandleAsync")!, ApplicationServices = app.Services };
                var guard = SelectionGuardFactory.Create(factoryContext, invocation => new ExistingCommandOutcomeFilter().InvokeAsync(invocation, _ => ValueTask.FromResult<object?>(TypedResults.Ok(1))));
                await guard(new DefaultEndpointFilterInvocationContext(context, request, operations, marker, context, CancellationToken.None));
            }

            if (unexpected) { await Assert.ThrowsAsync<UnexpectedResourceException>(Invoke); }
            else { await Assert.ThrowsAsync<InvalidResourceException>(Invoke); }

            Assert.Equal(0, Probe(app).OwnershipChecks);
            Assert.Equal(0, Probe(app).Operations);
        }
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("cancel")]
    public async Task Unknown_command_failures_and_cancellation_still_propagate(string outcome)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var route in StartRoutes())
            {
                if (outcome == "unknown") { await Assert.ThrowsAsync<UnexpectedResourceException>(() => client.PostAsJsonAsync(route, new GuardedStartRequest(new[] { 1 }, outcome))); }
                else { await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.PostAsJsonAsync(route, new GuardedStartRequest(new[] { 1 }, outcome))); }
            }
        }
    }

    [Theory]
    [InlineData("guard", 403)]
    [InlineData("binding", 400)]
    [InlineData("unsupported", 415)]
    public async Task Earlier_native_rejection_skips_the_factory_guard_and_application_operations(string scenario, int status)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var route in StartRoutes())
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, route);
                request.Content = new StringContent(scenario == "binding" ? "{broken" : scenario == "unsupported" ? "plain" : "{\"items\":[1]}", Encoding.UTF8, scenario == "unsupported" ? "text/plain" : "application/json");
                if (scenario == "guard") { request.Headers.Add("X-Sample-Block", "true"); }

                using var response = await client.SendAsync(request);
                Assert.Equal(status, (int)response.StatusCode);
                Assert.False(response.Headers.Contains("X-Factory-Scope"));
                Assert.Equal(0, Probe(app).OwnershipChecks);
                Assert.Equal(0, Probe(app).Operations);
            }
        }
    }

    [Fact]
    public async Task Native_pipeline_builds_observe_original_signatures_and_warmed_requests_do_not_rebuild()
    {
        await using var app = Create(false);
        await app.StartAsync();
        _ = app.Services.GetRequiredService<EndpointDataSource>().Endpoints;
        var ledger = app.Services.GetRequiredService<FactoryBuildLedger>();
        var expected = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [typeof(GuardedStartEndpoint).FullName!] = 0,
            [typeof(FactoryNamespaces.NamespaceGuardedStartEndpoint).FullName!] = 1,
            [typeof(ShiftedGuardedStartBase).FullName!] = 2
        };

        void ValidateBuildRecords()
        {
            var entries = ledger.Entries.ToArray();
            Assert.True(entries.Length >= 4);
            Assert.All(entries, entry =>
            {
                Assert.True(expected.TryGetValue(entry.DeclaringType, out var index));
                Assert.Equal(index, entry.ParameterIndex);
                Assert.Equal("HandleAsync", entry.MethodName);
            });
            Assert.All(expected.Keys, owner => Assert.Contains(entries, entry => entry.DeclaringType == owner));
            Assert.True(entries.Count(entry => entry.DeclaringType == typeof(ShiftedGuardedStartBase).FullName) >= 2);
        }

        ValidateBuildRecords();
        using var client = app.GetTestClient();
        var seenScopes = new HashSet<Guid>();

        async Task DispatchAsync(string route)
        {
            using var response = await client.PostAsJsonAsync(route, new GuardedStartRequest(new[] { 1 }));
            Assert.Equal(200, (int)response.StatusCode);
            var result = (await response.Content.ReadFromJsonAsync<GuardedStartedDto>())!;
            Assert.Equal(result.Scope.ToString(), Assert.Single(response.Headers.GetValues("X-Factory-Scope")));
            Assert.True(seenScopes.Add(result.Scope));
        }

        // Native metadata enumeration and first dispatch may construct separate pipelines.
        foreach (var route in StartRoutes()) { await DispatchAsync(route); }

        ValidateBuildRecords();
        var warmedBuildCount = ledger.Entries.Count;
        foreach (var route in StartRoutes())
        {
            for (var iteration = 0; iteration < 2; iteration++)
            {
                await DispatchAsync(route);
            }
        }

        Assert.Equal(warmedBuildCount, ledger.Entries.Count);
        ValidateBuildRecords();
        Assert.Null(Route(app, "/sample/start-alias", "POST").Metadata.GetMetadata<IEndpointNameMetadata>());
    }

    [Fact]
    public async Task An_early_filter_result_retains_the_original_concrete_outer_union()
    {
        await using var app = EmptyHost();
        var context = new DefaultHttpContext();
        var request = new GuardedStartRequest(null);
        var factoryContext = new EndpointFilterFactoryContext { ApplicationServices = app.Services, MethodInfo = typeof(GuardedStartEndpoint).GetMethod("HandleAsync")! };
        var guard = SelectionGuardFactory.Create(factoryContext, _ => throw new InvalidOperationException("An early rejection must skip next."));
        var actual = await guard(new DefaultEndpointFilterInvocationContext(context, request, null!, null!, context, CancellationToken.None));
        var reference = await LegacyGuardedStarts.BeforeCommand(request, null!, null!, context, CancellationToken.None);
        Assert.Equal(reference.GetType(), actual!.GetType());
        var problem = Assert.IsType<ProblemHttpResult>(((INestedHttpResult)actual).Result);
        Assert.Equal(400, problem.StatusCode);
        Assert.Equal("invalid-selection", problem.ProblemDetails.Extensions["code"]);
    }

    [Fact]
    public async Task Repeated_class_method_factories_and_regular_filters_use_native_order()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            for (var iteration = 0; iteration < 2; iteration++)
            {
                using var response = await client.GetAsync("/sample/factory-repeated");
                Assert.Equal(2, await response.Content.ReadFromJsonAsync<int>());
                Assert.Equal("2", Assert.Single(response.Headers.GetValues("X-Factory-Count")));
                Assert.Equal("true", Assert.Single(response.Headers.GetValues("X-Regular-Entered")));
                Assert.Equal("1", Assert.Single(response.Headers.GetValues("X-Regular-Factory-Count")));
            }
        }
    }

    [Fact]
    public async Task Original_nested_result_metadata_and_API_descriptions_match_the_legacy_registration()
    {
        await using var legacy = Create(true);
        await using var medium = Create(false);
        await legacy.StartAsync();
        await medium.StartAsync();
        Assert.Equal(Descriptions(legacy), Descriptions(medium));
        foreach (var route in StartRoutes())
        {
            var actual = Route(medium, route, "POST");
            Assert.Equal(Responses(Route(legacy, route, "POST")), Responses(actual));
            Assert.Equal(typeof(Task<Results<Results<NotFound, Ok<GuardedStartedDto>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>>), actual.Metadata.GetMetadata<MethodInfo>()!.ReturnType);
        }

        Assert.Equal(typeof(ShiftedGuardedStartBase), Route(medium, "/sample/shifted-start", "POST").Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Fluent_endpoint_and_group_factories_keep_declared_metadata_and_Configure_order(bool groupRegistration)
    {
        await using var app = EmptyHost();
        var group = app.MapGroup("/fluent");
        if (groupRegistration) { group.AddMediumApiFilterFactory<DeclaredFactory>(); }

        var endpoint = group.MapPost("/described", FactoryTargets.Described).WithMetadata(new EndpointResponseAttribute(419, typeof(string), "text/plain"));
        endpoint.AddEndpointFilter<FactoryOrderFilter>();
        if (!groupRegistration) { endpoint.AddMediumApiFilterFactory<DeclaredFactory>(); }

        await app.StartAsync();
        var route = Route(app, "/fluent/described", "POST");
        Assert.Equal(new[] { "configure", "declared" }, route.Metadata.GetOrderedMetadata<FactoryStageAttribute>().Select(stage => stage.Value));
        Assert.Single(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 418);
        var fallback = Assert.Single(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 419);
        Assert.Equal(typeof(string), fallback.Type);
        Assert.Equal(new[] { "text/plain" }, fallback.ContentTypes);
        Assert.Contains(route.Metadata.GetOrderedMetadata<IAcceptsMetadata>(), accepts => accepts.RequestType == typeof(GuardedStartRequest) && accepts.ContentTypes.Contains("application/json"));
        Assert.False(route.Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        using var client = app.GetTestClient();
        using var response = await client.PostAsJsonAsync("/fluent/described", new GuardedStartRequest(new[] { 1 }));
        Assert.Equal(200, (int)response.StatusCode);
        Assert.Equal("present", Assert.Single(response.Headers.GetValues("X-Factory-Declaration")));
        Assert.Equal(nameof(FactoryTargets.Described), Assert.Single(response.Headers.GetValues("X-Factory-Created-For")));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("X-Regular-Entered")));
    }

    [Fact]
    public async Task Fluent_group_and_endpoint_struct_factories_repeat_without_a_Configure_override()
    {
        await using var app = EmptyHost();
        var group = app.MapGroup("/fluent").AddMediumApiFilterFactory<RepeatedFactory>();
        group.MapGet("/repeated", FactoryTargets.Count).AddEndpointFilter<FactoryOrderFilter>().AddMediumApiFilterFactory<RepeatedFactory>();
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/fluent/repeated");
        Assert.Equal(2, await response.Content.ReadFromJsonAsync<int>());
        Assert.Equal("2", Assert.Single(response.Headers.GetValues("X-Factory-Count")));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("X-Regular-Entered")));
        Assert.Equal("1", Assert.Single(response.Headers.GetValues("X-Regular-Factory-Count")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Native_next_can_be_invoked_twice_without_a_new_pipeline(bool mediumRegistration)
    {
        await using var app = EmptyHost();
        var endpoint = app.MapGet("/twice", FactoryTargets.Terminal);
        if (mediumRegistration) { endpoint.AddMediumApiFilterFactory<NextTwiceFactory>(); }
        else { endpoint.AddEndpointFilterFactory(NextTwiceFactory.Create); }

        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/twice");
        Assert.Equal(2, await response.Content.ReadFromJsonAsync<int>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Null_builders_fail_before_the_factory_Configure_convention(bool group)
    {
        if (group) { Assert.Throws<ArgumentNullException>(() => ((RouteGroupBuilder)null!).AddMediumApiFilterFactory<ThrowingConfigureFactory>()); }
        else { Assert.Throws<ArgumentNullException>(() => ((RouteHandlerBuilder)null!).AddMediumApiFilterFactory<ThrowingConfigureFactory>()); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Configure_failure_retains_its_immediate_native_registration_boundary(bool group)
    {
        await using var app = EmptyHost();
        if (group) { Assert.Throws<FactoryConventionException>(() => app.MapGroup("/bad").AddMediumApiFilterFactory<ThrowingConfigureFactory>()); }
        else { Assert.Throws<FactoryConventionException>(() => app.MapGet("/bad", FactoryTargets.Terminal).AddMediumApiFilterFactory<ThrowingConfigureFactory>()); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_failure_propagates_from_native_endpoint_materialization(bool mediumRegistration)
    {
        await using var app = EmptyHost();
        var endpoint = app.MapGet("/bad", FactoryTargets.Terminal);
        if (mediumRegistration) { endpoint.AddMediumApiFilterFactory<ThrowingCreateFactory>(); }
        else { endpoint.AddEndpointFilterFactory(ThrowingCreateFactory.Create); }

        Assert.Throws<FactoryCreationException>(() => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToArray());
    }

    [Fact]
    public async Task Missing_original_parameter_fails_at_build_with_the_original_handler_identity()
    {
        await using var app = EmptyHost();
        app.MapMediumApiEndpoint<MissingGuardRequestEndpoint>();
        var error = Assert.Throws<InvalidOperationException>(() => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToArray());
        Assert.Equal($"Expected one GuardedStartRequest parameter on {typeof(MissingGuardRequestEndpoint).FullName}.HandleAsync; found 0.", error.Message);
    }

    [Fact]
    public async Task Ambiguous_original_parameter_lookup_does_not_silently_pick_an_argument()
    {
        await using var app = EmptyHost();
        var context = new EndpointFilterFactoryContext { ApplicationServices = app.Services, MethodInfo = typeof(FactoryTargets).GetMethod(nameof(FactoryTargets.Ambiguous))! };
        var error = Assert.Throws<InvalidOperationException>(() => SelectionGuardFactory.Create(context, _ => ValueTask.FromResult<object?>(TypedResults.Ok(1))));
        Assert.EndsWith(".Ambiguous; found 2.", error.Message, StringComparison.Ordinal);
    }

    private static string[] StartRoutes()
    {
        return new[] { "/sample/guarded-start", "/sample/shifted-start", "/sample/start-alias", "/sample/factory-namespace/start" };
    }

    private static WebApplication EmptyHost()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton<FactoryBuildLedger>();
        builder.Services.AddScoped<FactoryRequestScope>();
        return builder.Build();
    }

    private static async Task AssertProblem(HttpResponseMessage response, string code, string title)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        Assert.Equal(title, json.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain("Internal synthetic", json.RootElement.GetRawText(), StringComparison.Ordinal);
    }

    private sealed class ThrowingItems(bool unexpected) : IReadOnlyList<int>
    {
        public int Count
        {
            get
            {
                if (unexpected) { throw new UnexpectedResourceException(); }

                throw new InvalidResourceException("early-code");
            }
        }

        public int this[int index]
        {
            get { throw new NotSupportedException(); }
        }

        public IEnumerator<int> GetEnumerator()
        {
            return Enumerable.Empty<int>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

public static class FactoryTargets
{
    public static Ok<int> Described(GuardedStartRequest request)
    {
        return TypedResults.Ok(request.Items!.Count);
    }

    public static Ok<int> Count(HttpContext context)
    {
        return TypedResults.Ok((int)context.Items["factory-count"]!);
    }

    public static Ok<int> Terminal(HttpContext context)
    {
        var count = context.Items.TryGetValue("terminal-count", out var value) ? (int)value! : 0;
        context.Items["terminal-count"] = count + 1;
        return TypedResults.Ok(count + 1);
    }

    public static Ok<int> Ambiguous(GuardedStartRequest first, GuardedStartRequest second)
    {
        return TypedResults.Ok(0);
    }
}

public sealed class FactoryConventionException : Exception;

public sealed class FactoryCreationException : Exception;

public sealed class ThrowingConfigureFactory : IMediumApiEndpointFilterFactory
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        throw new FactoryConventionException();
    }

    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        return next;
    }
}

public sealed class ThrowingCreateFactory : IMediumApiEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        throw new FactoryCreationException();
    }
}