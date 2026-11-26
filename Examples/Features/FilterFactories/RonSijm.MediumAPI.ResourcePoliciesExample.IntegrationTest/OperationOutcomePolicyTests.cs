using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using static RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest.ResourcePoliciesHost;

namespace RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest;

public sealed class OperationOutcomePolicyTests
{
    [Theory]
    [InlineData("conflict", "idempotency-key-reused", "This idempotency key was already used for a different request.")]
    [InlineData("pending", "idempotency-request-in-progress", "A request with this idempotency key is still being processed.")]
    [InlineData("receipt", "idempotency-one-time-receipt-issued", "The token was created. Check saved tokens; its one-time secret cannot be shown again.")]
    [InlineData("custom-conflict", "custom-conflict", "This idempotency key was already used for a different request.")]
    public async Task Selected_conflict_codes_and_titles_match_the_original_inner_policies(string outcome, string code, string title)
    {
        var references = new Dictionary<string, Contract>();
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var action in new[] { "customers", "projects", "reports" })
            {
                using var response = await Post(client, action, outcome);
                Assert.Equal(409, (int)response.StatusCode);
                Assert.Null(response.Headers.CacheControl);
                Assert.False(response.Headers.Contains("Idempotent-Replayed"));
                var contract = await Capture(response);
                Problem(contract, code, title);
                if (legacy) { references[action] = contract; }
                else { Equal(references[action], contract); }
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Dynamic_and_typed_successes_keep_payloads_Locations_and_replay_headers(bool replayed)
    {
        var references = new Dictionary<string, Contract>();
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var action in new[] { "customers", "projects", "reports" })
            {
                using var response = await Post(client, action, replayed ? "replay" : "ok");
                Assert.Equal(action == "reports" ? 200 : 201, (int)response.StatusCode);
                Assert.Equal(action == "reports", response.Headers.CacheControl?.NoStore == true);
                Assert.Equal(replayed, response.Headers.Contains("Idempotent-Replayed"));
                if (replayed) { Assert.Equal("true", Assert.Single(response.Headers.GetValues("Idempotent-Replayed"))); }

                var contract = await Capture(response);
                if (legacy) { references[action] = contract; }
                else { Equal(references[action], contract); }

                using var json = JsonDocument.Parse(contract.Body);
                Assert.Equal(17, json.RootElement.GetProperty(action == "reports" ? "reportId" : "id").GetInt64());
                Assert.Equal(action == "reports" ? null : "/sample/outcomes/" + action + "/17", contract.Location);
                if (action == "reports")
                {
                    Assert.Equal(5, json.RootElement.GetProperty("analysisRunId").GetInt64());
                    Assert.Equal(3, json.RootElement.GetProperty("projectVersionId").GetInt64());
                }
            }
        }
    }

    [Theory]
    [InlineData("validation")]
    [InlineData("coded-validation")]
    public async Task Outer_validation_policies_keep_fixed_versus_coded_contracts(string outcome)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var action in new[] { "customers", "projects", "reports" })
            {
                using var response = await Post(client, action, outcome);
                Assert.Equal(400, (int)response.StatusCode);
                Problem(await Capture(response), action == "reports" && outcome == "coded-validation" ? "draft-not-ready" : "invalid-request", "The request could not be completed.");
                Assert.Null(response.Headers.CacheControl);
            }
        }
    }

    [Theory]
    [InlineData("forbidden", 403)]
    [InlineData("missing", 404)]
    public async Task Existing_outer_policies_keep_forbidden_and_missing_results_distinct(string outcome, int status)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var action in Actions().Where(action => outcome != "missing" || action != "customers"))
            {
                using var response = await Post(client, action, outcome);
                Assert.Equal(status, (int)response.StatusCode);
                Assert.Empty(await response.Content.ReadAsByteArrayAsync());
                Assert.Null(response.Headers.CacheControl);
                Assert.False(response.Headers.Contains("Idempotent-Replayed"));
            }
        }
    }

    [Theory]
    [InlineData("ok", 204)]
    [InlineData("ownership-missing", 404)]
    [InlineData("completed", 409)]
    public async Task Cancellation_preserves_owned_resource_order_and_its_domain_conflict(string outcome, int status)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Post(client, "operations/7/cancel", outcome);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal(1, Probe(app).OwnershipChecks);
            Assert.Equal(outcome == "ok" ? 1 : 0, Probe(app).Operations);
            Assert.Equal(outcome == "ok" ? "ownership,operation" : "ownership", Assert.Single(response.Headers.GetValues("X-Resource-Trace")));
            if (outcome == "completed") { Problem(await Capture(response), "operation-already-completed", "The operation cannot be cancelled."); }
            else { Assert.Empty(await response.Content.ReadAsByteArrayAsync()); }
        }
    }

    [Theory]
    [InlineData("ok", false, 201)]
    [InlineData("ok", true, 200)]
    [InlineData("concurrency", false, 409)]
    public async Task Catalog_saves_keep_success_selected_headers_and_specific_stale_problem(string outcome, bool update, int status)
    {
        Contract? reference = null;
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Post(client, "catalog", outcome, update);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal(outcome == "ok", response.Headers.CacheControl?.NoStore == true);
            var contract = await Capture(response);
            if (legacy) { reference = contract; }
            else { Equal(reference!, contract); }

            if (outcome == "concurrency") { Problem(contract, "stale-catalog-entry", "Reload the current entry before saving."); }
            else
            {
                using var json = JsonDocument.Parse(contract.Body);
                Assert.Equal(17, json.RootElement.GetProperty("id").GetInt64());
                Assert.Equal(3, json.RootElement.GetProperty("revision").GetInt64());
                Assert.Equal(update ? null : "/sample/outcomes/catalog/17", contract.Location);
            }
        }
    }

    [Theory]
    [InlineData("ok", 202)]
    [InlineData("ownership-missing", 404)]
    [InlineData("coded-validation", 400)]
    public async Task Intake_keeps_ownership_before_Accepted_and_coded_validation(string outcome, int status)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Post(client, "intake/7", outcome);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal(1, Probe(app).OwnershipChecks);
            Assert.Equal(outcome == "ok" ? 1 : 0, Probe(app).Operations);
            if (outcome == "ok")
            {
                Assert.Equal("/sample/outcomes/intake/7/generation", response.Headers.Location?.OriginalString);
                using var json = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());
                Assert.Equal(17, json.RootElement.GetProperty("id").GetInt64());
                Assert.Equal("Queued", json.RootElement.GetProperty("state").GetString());
            }
            else if (outcome == "coded-validation") { Problem(await Capture(response), "draft-not-ready", "The request could not be completed."); }
            else { Assert.Empty(await response.Content.ReadAsByteArrayAsync()); }
        }
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("cancel")]
    public async Task Unknown_failures_and_cancellation_still_propagate_for_every_outcome_adapter(string outcome)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var action in Actions())
            {
                if (outcome == "unknown") { await Assert.ThrowsAsync<UnexpectedResourceException>(() => Post(client, action, outcome)); }
                else { await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Post(client, action, outcome)); }
            }
        }
    }

    [Theory]
    [InlineData("customers", "missing")]
    [InlineData("operations/7/cancel", "validation")]
    [InlineData("intake/7", "concurrency")]
    public async Task Specialized_adapters_do_not_acquire_another_policy_catch(string action, string outcome)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            if (outcome == "missing") { await Assert.ThrowsAsync<MissingResourceException>(() => Post(client, action, outcome)); }
            else if (outcome == "validation") { await Assert.ThrowsAsync<InvalidResourceException>(() => Post(client, action, outcome)); }
            else { await Assert.ThrowsAsync<StaleResourceException>(() => Post(client, action, outcome)); }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_replay_header_already_written_survives_a_later_failure(bool unknown)
    {
        foreach (var legacy in new[] { true, false })
        {
            var context = new DefaultHttpContext();
            Func<Task<Created<CustomerOutcomeDto>>> operation = () =>
            {
                context.Response.Headers["Idempotent-Replayed"] = "true";
                return Task.FromException<Created<CustomerOutcomeDto>>(unknown ? new UnexpectedResourceException() : new InvalidResourceException("late-code"));
            };

            async Task<object?> Invoke()
            {
                if (legacy) { return await LegacyOperationOutcomePolicies.CustomerAsync(operation); }

                return await new CustomerCreateOutcomeFilter().InvokeAsync(new DefaultEndpointFilterInvocationContext(context), async _ => await operation());
            }

            if (unknown) { await Assert.ThrowsAsync<UnexpectedResourceException>(Invoke); }
            else
            {
                var result = await Invoke();
                var problem = legacy ? Assert.IsType<ProblemHttpResult>(((INestedHttpResult)result!).Result) : Assert.IsType<ProblemHttpResult>(result);
                Assert.Equal("invalid-request", problem.ProblemDetails.Extensions["code"]);
            }

            Assert.Equal("true", context.Response.Headers["Idempotent-Replayed"].ToString());
        }
    }

    [Theory]
    [InlineData("guard", 403)]
    [InlineData("binding", 400)]
    [InlineData("unsupported", 415)]
    public async Task Native_guards_and_binding_keep_rejection_outside_the_operation(string scenario, int status)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var action in Actions())
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/sample/outcomes/" + action);
                request.Content = new StringContent(scenario == "binding" ? "{broken" : scenario == "unsupported" ? "plain" : "{\"outcome\":\"ok\"}", Encoding.UTF8, scenario == "unsupported" ? "text/plain" : "application/json");
                if (scenario == "guard") { request.Headers.Add("X-Sample-Block", "true"); }

                using var response = await client.SendAsync(request);
                Assert.Equal(status, (int)response.StatusCode);
                Assert.False(response.Headers.Contains("Idempotent-Replayed"));
                Assert.Null(response.Headers.CacheControl);
                Assert.Equal(0, Probe(app).Operations);
                Assert.Equal(0, Probe(app).OwnershipChecks);
            }
        }
    }

    [Fact]
    public async Task Response_metadata_preserves_typed_unions_omissions_and_the_existing_media_mismatch()
    {
        await using var legacy = Create(true);
        await using var medium = Create(false);
        await legacy.StartAsync();
        await medium.StartAsync();
        Assert.Equal(Descriptions(legacy), Descriptions(medium));
        foreach (var pattern in new[] { "/sample/outcomes/customers", "/sample/outcomes/projects", "/sample/outcomes/reports", "/sample/outcomes/operations/{resourceId:int}/cancel", "/sample/outcomes/catalog", "/sample/outcomes/intake/{resourceId:int}" })
        {
            var reference = Route(legacy, pattern, "POST");
            var actual = Route(medium, pattern, "POST");
            Assert.Equal(Responses(reference), Responses(actual));
            Assert.Equal(reference.Metadata.GetMetadata<MethodInfo>()!.ReturnType, actual.Metadata.GetMetadata<MethodInfo>()!.ReturnType);
        }

        var customer = Route(medium, "/sample/outcomes/customers", "POST");
        Assert.DoesNotContain(customer.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 404);
        Assert.Equal(typeof(Task<Results<Created<CustomerOutcomeDto>, ForbidHttpResult, ProblemHttpResult>>), customer.Metadata.GetMetadata<MethodInfo>()!.ReturnType);
        var intake = Route(medium, "/sample/outcomes/intake/{resourceId:int}", "POST");
        Assert.Equal(typeof(Task<Results<Accepted<GenerationOutcomeDto>, NotFound, ProblemHttpResult>>), intake.Metadata.GetMetadata<MethodInfo>()!.ReturnType);
        var declaredConflict = Assert.Single(Route(medium, "/sample/outcomes/catalog", "POST").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 409);
        Assert.Equal(new[] { "application/json" }, declaredConflict.ContentTypes);
        using var client = medium.GetTestClient();
        using var conflict = await Post(client, "catalog", "concurrency");
        Assert.Equal("application/problem+json", conflict.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void Specialized_outcome_filters_add_no_responses_or_fallbacks()
    {
        foreach (var filter in new[] { typeof(ReplayConflictOutcomeFilter), typeof(CustomerCreateOutcomeFilter), typeof(CompletedOperationOutcomeFilter), typeof(StaleCatalogOutcomeFilter), typeof(IntakeValidationOutcomeFilter) })
        {
            Assert.Empty(filter.GetCustomAttributes<EndpointResponseAttribute>(false));
            Assert.Empty(filter.GetCustomAttributes<EndpointResponseFallbackAttribute>(false));
        }
    }

    private static string[] Actions()
    {
        return ["customers", "projects", "reports", "operations/7/cancel", "catalog", "intake/7"];
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string action, string outcome, bool update = false)
    {
        return client.PostAsJsonAsync("/sample/outcomes/" + action, new OperationOutcomeCommand(outcome, update));
    }

    private static async Task<Contract> Capture(HttpResponseMessage response)
    {
        return new Contract(await response.Content.ReadAsByteArrayAsync(), response.Content.Headers.ContentType?.MediaType, response.Headers.Location?.OriginalString);
    }

    private static void Equal(Contract expected, Contract actual)
    {
        Assert.Equal(expected.Body, actual.Body);
        Assert.Equal(expected.MediaType, actual.MediaType);
        Assert.Equal(expected.Location, actual.Location);
    }

    private static void Problem(Contract response, string code, string title)
    {
        Assert.Equal("application/problem+json", response.MediaType);
        using var json = JsonDocument.Parse(response.Body);
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        Assert.Equal(title, json.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain("Internal synthetic", Encoding.UTF8.GetString(response.Body), StringComparison.Ordinal);
    }

    private sealed record Contract(byte[] Body, string? MediaType, string? Location);
}