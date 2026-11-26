using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using RonSijm.MediumAPI.ResponseHeadersExample.Endpoints.Multiple;
using Xunit;

namespace RonSijm.MediumAPI.ResponseHeadersExample.IntegrationTest;

public sealed class MultipleResultHeaderTests
{
    [Fact]
    public void Selector_lists_copy_assignment_and_getter_arrays_and_null_restores_the_unset_state()
    {
        var declaration = new ResponseHeaderAttribute("X-Choice", "selected");
        Assert.Null(declaration.ResultTypes);
        var input = new[] { typeof(Ok<>), typeof(Created<>) };
        declaration.ResultTypes = input;
        input[0] = typeof(ProblemHttpResult);
        var exposed = declaration.ResultTypes!;
        exposed[1] = typeof(ProblemHttpResult);
        Assert.Equal(new[] { typeof(Ok<>), typeof(Created<>) }, declaration.ResultTypes);
        declaration.ResultTypes = null;
        Assert.Null(declaration.ResultTypes);
    }

    [Fact]
    public void Invalid_lists_are_rejected_without_overwriting_the_previous_valid_selector_list()
    {
        var declaration = new ResponseHeaderAttribute("X-Choice", "selected") { ResultTypes = [typeof(Ok<>)] };
        var partial = typeof(GenericResult<,>).MakeGenericType(typeof(string), typeof(List<>));
        Assert.True(partial.ContainsGenericParameters);
        Assert.False(partial.IsGenericTypeDefinition);
        foreach (var invalid in new[] { Array.Empty<Type>(), new Type[] { null! }, new[] { typeof(string) }, new[] { partial } })
        {
            Assert.Throws<ArgumentException>(() => declaration.ResultTypes = invalid);
            Assert.Equal(new[] { typeof(Ok<>) }, declaration.ResultTypes);
        }
    }

    [Fact]
    public async Task Singular_and_plural_selectors_conflict_at_registration_and_can_be_cleared_explicitly()
    {
        await using var app = Create();
        var declaration = new ResponseHeaderAttribute("X-Choice", "selected") { ResultType = typeof(Ok<>), ResultTypes = [typeof(Created<>)] };
        var route = app.MapGet("/extra/conflict", MultipleResultBodies.Fluent);
        Assert.Throws<InvalidOperationException>(() => route.WithResponseHeader(declaration));
        declaration.ResultTypes = null;
        route.WithResponseHeader(declaration);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/extra/conflict");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("selected", Header(response, "X-Choice"));
    }

    [Fact]
    public async Task Registration_snapshots_survive_later_attribute_array_mutation_and_are_isolated_between_hosts()
    {
        await using var first = Create();
        await using var second = Create();
        var input = new[] { typeof(Ok<>), typeof(Created<>) };
        var firstPolicy = new ResponseHeaderAttribute("X-Choice", "first") { ResultTypes = input };
        var secondPolicy = new ResponseHeaderAttribute("X-Choice", "second") { ResultTypes = input };
        first.MapGet("/extra/owned", MultipleResultBodies.Fluent).WithResponseHeader(firstPolicy);
        second.MapGet("/extra/owned", MultipleResultBodies.Fluent).WithResponseHeader(secondPolicy);
        second.MapGet("/extra/unselected", () => TypedResults.Problem(statusCode: 409)).WithResponseHeader(secondPolicy);
        input[0] = typeof(ProblemHttpResult);
        firstPolicy.ResultTypes![0] = typeof(ProblemHttpResult);
        firstPolicy.ResultTypes = [typeof(ProblemHttpResult)];
        secondPolicy.ResultTypes = null;
        await first.StartAsync();
        await second.StartAsync();
        using var firstClient = first.GetTestClient();
        using var secondClient = second.GetTestClient();
        using var firstResponse = await firstClient.GetAsync("/extra/owned");
        using var secondResponse = await secondClient.GetAsync("/extra/owned");
        Assert.Equal("first", Header(firstResponse, "X-Choice"));
        Assert.Equal("second", Header(secondResponse, "X-Choice"));
        using var unselected = await secondClient.GetAsync("/extra/unselected");
        Assert.False(unselected.Headers.Contains("X-Choice"));
    }

    [Fact]
    public async Task All_owner_scopes_preserve_native_typed_metadata_names_and_actual_API_Explorer_descriptions()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        await app.StartAsync();
        await native.StartAsync();
        Assert.Equal(3, app.GetMediumApiContractSnapshot().Routes.Count);
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
        var provider = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var nativeProvider = native.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var descriptions = provider.GetMediumApiDescriptionSnapshot();
        var nativeDescriptions = nativeProvider.GetMediumApiDescriptionSnapshot();
        Assert.Equal(3, descriptions.Operations.Count);
        Assert.Equal(3, nativeDescriptions.Operations.Count);
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(nativeDescriptions, descriptions));
    }

    [Theory]
    [InlineData("ok", HttpStatusCode.OK, true)]
    [InlineData("created", HttpStatusCode.Created, true)]
    [InlineData("problem", HttpStatusCode.BadRequest, false)]
    public async Task Class_method_group_and_filter_OR_policies_preserve_upsert_payloads_and_problem_boundaries(string kind, HttpStatusCode status, bool selected)
    {
        string? expectedBody = null;
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.PutAsync("/multiple-results/upsert/" + kind, null);
            Assert.Equal(status, response.StatusCode);
            Assert.Equal("before", Header(response, "X-Multiple-Before"));
            Assert.Equal(selected, response.Headers.Contains("X-Multiple-Class"));
            Assert.Equal(selected, response.Headers.Contains("X-Multiple-Group"));
            Assert.Equal(selected, response.Headers.Contains("X-Multiple-Filter"));
            Assert.Equal(selected, response.Headers.Contains("Cache-Control"));
            if (selected)
            {
                Assert.Equal("class", Header(response, "X-Multiple-Class"));
                Assert.Equal("group", Header(response, "X-Multiple-Group"));
                Assert.Equal("filter", Header(response, "X-Multiple-Filter"));
                Assert.Equal("no-store", Header(response, "Cache-Control"));
                Assert.Equal(new MultipleResultReply(kind), await response.Content.ReadFromJsonAsync<MultipleResultReply>());
            }

            Assert.Equal(kind == "created" ? "/multiple-results/items/7" : null, response.Headers.Location?.ToString());
            Assert.Equal(selected ? "application/json" : "application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync();
            if (native)
            {
                expectedBody = body;
            }
            else
            {
                Assert.Equal(expectedBody, body);
            }
        }
    }

    [Fact]
    public async Task A_created_filter_short_circuit_receives_its_filter_and_group_policies_without_handler_policies()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.PutAsync("/multiple-results/upsert/problem?block=true", null);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(new MultipleResultReply("blocked"), await response.Content.ReadFromJsonAsync<MultipleResultReply>());
            Assert.Equal("/multiple-results/items/blocked", response.Headers.Location?.ToString());
            Assert.Equal("filter", Header(response, "X-Multiple-Filter"));
            Assert.Equal("group", Header(response, "X-Multiple-Group"));
            Assert.False(response.Headers.Contains("X-Multiple-Class"));
            Assert.False(response.Headers.Contains("Cache-Control"));
        }
    }

    [Theory]
    [InlineData("fault")]
    [InlineData("cancel")]
    public async Task Faults_and_cancellation_keep_before_next_defaults_without_inventing_a_selected_result(string kind)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            if (kind == "cancel")
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.PutAsync("/multiple-results/upsert/" + kind, null));
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => client.PutAsync("/multiple-results/upsert/" + kind, null));
            }

            var captured = Assert.IsType<HeaderDictionary>(Observation(app).LastHeaders);
            Assert.Equal("before", captured["X-Multiple-Before"].ToString());
            Assert.False(captured.ContainsKey("X-Multiple-Class"));
            Assert.False(captured.ContainsKey("X-Multiple-Group"));
            Assert.False(captured.ContainsKey("X-Multiple-Filter"));
            Assert.False(captured.ContainsKey("Cache-Control"));
        }
    }

    [Fact]
    public async Task Native_query_binding_failure_does_not_acquire_handler_success_policies()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.PutAsync("/multiple-results/upsert/ok?block=invalid", null);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("before", Header(response, "X-Multiple-Before"));
            Assert.False(response.Headers.Contains("X-Multiple-Class"));
            Assert.False(response.Headers.Contains("X-Multiple-Filter"));
            Assert.False(response.Headers.Contains("X-Multiple-Group"));
            Assert.False(response.Headers.Contains("Cache-Control"));
        }
    }

    [Fact]
    public async Task Matching_result_lists_skip_header_mutation_after_the_response_has_started()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/multiple-results/started");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("started:complete", await response.Content.ReadAsStringAsync());
            Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("before", Header(response, "X-Multiple-Before"));
            Assert.False(response.Headers.Contains("X-Multiple-Started"));
        }
    }

    [Fact]
    public async Task A_fluent_policy_applies_once_even_when_interface_open_and_closed_selectors_all_match()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/multiple-results/fluent");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("once", Header(response, "X-Multiple-Match"));
            Assert.Equal(1, Observation(app).MatchingHeaderWrites);
            Assert.Equal(new MultipleResultReply("fluent"), await response.Content.ReadFromJsonAsync<MultipleResultReply>());
        }
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(31, true)]
    [InlineData(32, false)]
    public async Task Native_nested_result_traversal_keeps_its_existing_32_node_boundary(int wrappers, bool selected)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            IResult result = TypedResults.Ok(new MultipleResultReply("nested"));
            for (var index = 0; index < wrappers; index++)
            {
                result = new NestedResult(result);
            }

            var route = app.MapGet("/extra/nested", () => result);
            AddExtraPolicy(route, native, "X-Choice", IsReplySuccess);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/extra/nested");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(selected, response.Headers.Contains("X-Choice"));
            Assert.Equal(new MultipleResultReply("nested"), await response.Content.ReadFromJsonAsync<MultipleResultReply>());
        }
    }

    [Fact]
    public async Task A_cyclic_custom_nested_result_remains_unknown_instead_of_looping_or_executing_during_matching()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var result = new CyclicResult();
            var route = app.MapGet("/extra/cycle", () => result);
            AddExtraPolicy(route, native, "X-Choice", IsReplySuccess);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/extra/cycle");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.False(response.Headers.Contains("X-Choice"));
            Assert.Equal(1, result.Executions);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Plural_selectors_keep_existing_open_generic_base_and_interface_matching(bool useInterface)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var route = app.MapGet("/extra/generic", () => new DerivedGenericResult());
            if (native)
            {
                AddNativePolicy(route, "X-Choice", "selected", result => result is DerivedGenericResult);
            }
            else
            {
                route.WithResponseHeader(new ResponseHeaderAttribute("X-Choice", "selected") { ResultTypes = [typeof(Created<>), useInterface ? typeof(IGenericResult<>) : typeof(GenericResult<,>)] });
            }

            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/extra/generic");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("selected", Header(response, "X-Choice"));
        }
    }

    [Fact]
    public async Task Result_lists_select_explicit_problem_types_rather_than_assuming_only_success_statuses()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var route = app.MapGet("/extra/problem", () => TypedResults.Problem(statusCode: 409, title: "Synthetic conflict."));
            if (native)
            {
                AddNativePolicy(route, "X-Choice", "selected", result => result is Created<MultipleResultReply> or ProblemHttpResult);
            }
            else
            {
                route.WithResponseHeader(new ResponseHeaderAttribute("X-Choice", "selected") { ResultTypes = [typeof(Created<>), typeof(ProblemHttpResult)] });
            }

            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/extra/problem");
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("selected", Header(response, "X-Choice"));
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task An_unset_plural_selector_keeps_unconditional_before_next_behavior_on_a_problem_response()
    {
        await using var app = Create();
        app.MapGet("/extra/unconditional", () => TypedResults.Problem(statusCode: 409, title: "Synthetic conflict.")).WithResponseHeader(new ResponseHeaderAttribute("X-Choice", "before") { ResultTypes = null });
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/extra/unconditional");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("before", Header(response, "X-Choice"));
    }

    private static WebApplication Create(bool native = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(MultipleResultsGroupDefinition).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory, EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
        builder.Services.AddSingleton(new HeaderObservation());
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            var observation = Observation(app);
            var feature = context.Features.Get<IHttpResponseFeature>()!;
            feature.Headers = new CountingHeaders(feature.Headers, observation);
            try
            {
                await next(context);
            }
            finally
            {
                observation.LastHeaders = new HeaderDictionary(context.Response.Headers.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase));
            }
        });
        app.UseRouting();
        if (native)
        {
            MapNative(app);
        }
        else
        {
            app.MapMediumApiNamespace<MultipleResultsGroupDefinition>();
        }

        return app;
    }

    private static void MapNative(WebApplication app)
    {
        var group = app.MapGroup("/multiple-results");
        group.AddEndpointFilterFactory((_, next) => context =>
        {
            context.HttpContext.Response.Headers["X-Multiple-Before"] = "before";
            return next(context);
        });
        AddNativePolicy(group, "X-Multiple-Group", "group", IsReplySuccess);
        var upsert = group.MapPut("/upsert/{kind}", NativeUpsert).WithName("UpsertMultipleResults");
        AddNativePolicy(upsert, "X-Multiple-Filter", "filter", IsReplySuccess);
        upsert.AddEndpointFilter<MultipleResultFilter>();
        upsert.AddEndpointFilterFactory((_, next) => async context =>
        {
            if (context.HttpContext.Response.StatusCode >= StatusCodes.Status400BadRequest)
            {
                return await next(context);
            }

            var result = await next(context);
            if (!context.HttpContext.Response.HasStarted && IsReplySuccess(result))
            {
                context.HttpContext.Response.Headers["X-Multiple-Class"] = "class";
                context.HttpContext.Response.Headers["Cache-Control"] = "no-store";
            }

            return result;
        });
        var fluent = group.MapGet("/fluent", MultipleResultBodies.Fluent).WithName("MultipleNativeFluent");
        AddNativePolicy(fluent, "X-Multiple-Match", "once", result => result is IResult);
        var started = group.MapGet("/started", (Func<HttpContext, Task<StartedReplyResult>>)MultipleResultBodies.Started).WithName("StartedMultipleResults");
        AddNativePolicy(started, "X-Multiple-Started", "late", result => result is StartedReplyResult);
    }

    private static Results<Results<Ok<MultipleResultReply>, Created<MultipleResultReply>>, ProblemHttpResult> NativeUpsert([FromRoute] string kind, [FromQuery] bool? block)
    {
        return MultipleResultBodies.Upsert(kind);
    }

    private static void AddNativePolicy(IEndpointConventionBuilder endpoint, string name, string value, Func<object?, bool> predicate)
    {
        endpoint.AddEndpointFilterFactory((_, next) => async context =>
        {
            var result = await next(context);
            if (!context.HttpContext.Response.HasStarted && predicate(result))
            {
                context.HttpContext.Response.Headers[name] = value;
            }

            return result;
        });
    }

    private static bool IsReplySuccess(object? result)
    {
        // An application-owned native oracle recognizes these two concrete outcomes, without consulting the library's selector matcher.
        for (var index = 0; result is not null && index < 32; index++)
        {
            if (result is Ok<MultipleResultReply> or Created<MultipleResultReply>)
            {
                return true;
            }

            result = (result as INestedHttpResult)?.Result;
        }

        return false;
    }

    private static void AddExtraPolicy(RouteHandlerBuilder route, bool native, string name, Func<object?, bool> predicate)
    {
        if (native)
        {
            AddNativePolicy(route, name, "selected", predicate);
        }
        else
        {
            route.WithResponseHeader(new ResponseHeaderAttribute(name, "selected") { ResultTypes = [typeof(Ok<>), typeof(Created<>)] });
        }
    }

    private static HeaderObservation Observation(WebApplication app)
    {
        return app.Services.GetRequiredService<HeaderObservation>();
    }

    private static string Header(HttpResponseMessage response, string name)
    {
        return Assert.Single(response.Headers.GetValues(name));
    }

    private static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    }

    private sealed class HeaderObservation
    {
        private int matchingHeaderWrites;
        public IHeaderDictionary? LastHeaders { get; set; }
        public int MatchingHeaderWrites => Volatile.Read(ref matchingHeaderWrites);

        public void Wrote(string name)
        {
            if (name.Equals("X-Multiple-Match", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref matchingHeaderWrites);
            }
        }
    }

    private sealed class CountingHeaders : HeaderDictionary, IHeaderDictionary
    {
        private readonly HeaderObservation observation;

        public CountingHeaders(IHeaderDictionary original, HeaderObservation observation) : base(original.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase))
        {
            this.observation = observation;
        }

        StringValues IHeaderDictionary.this[string key]
        {
            get { return base[key]; }
            set
            {
                observation.Wrote(key);
                base[key] = value;
            }
        }
    }

    private sealed class NestedResult : IResult, INestedHttpResult
    {
        public NestedResult(IResult result)
        {
            Result = result;
        }

        public IResult Result { get; }

        public Task ExecuteAsync(HttpContext context)
        {
            return Result.ExecuteAsync(context);
        }
    }

    private sealed class CyclicResult : IResult, INestedHttpResult
    {
        public IResult Result => this;
        public int Executions { get; private set; }

        public Task ExecuteAsync(HttpContext context)
        {
            Executions++;
            return TypedResults.NoContent().ExecuteAsync(context);
        }
    }

    private interface IGenericResult<T> : IResult { }

    private class GenericResult<TFirst, TSecond> : IResult
    {
        public Task ExecuteAsync(HttpContext context)
        {
            return TypedResults.Ok("generic").ExecuteAsync(context);
        }
    }

    private sealed class DerivedGenericResult : GenericResult<string, int>, IGenericResult<int> { }
}