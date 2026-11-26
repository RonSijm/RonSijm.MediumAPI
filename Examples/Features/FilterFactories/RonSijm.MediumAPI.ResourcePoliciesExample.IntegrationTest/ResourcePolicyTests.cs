using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using static RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest.ResourcePoliciesHost;

namespace RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest;

public sealed class ResourcePolicyTests
{
    [Theory]
    [InlineData("ok", 200, null)]
    [InlineData("forbidden", 403, null)]
    [InlineData("missing", 404, null)]
    [InlineData("concurrency", 409, "concurrency-conflict")]
    [InlineData("provider", 400, "repository-not-found")]
    [InlineData("validation", 400, "invalid-request")]
    [InlineData("coded-validation", 400, "invalid-request")]
    public async Task Repository_policy_keeps_six_handler_contracts_and_exact_safe_failures(string outcome, int status, string? code)
    {
        var references = new Dictionary<string, ResponseContract>();
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var action in new[] { "list", "save", "delete", "queue", "retry", "cancel" })
            {
                var beforeChecks = Probe(app).OwnershipChecks;
                var beforeOperations = Probe(app).Operations;
                using var response = await Command(client, action, outcome);
                Assert.Equal(outcome == "ok" ? action is "delete" or "retry" or "cancel" ? 204 : action == "queue" ? 202 : 200 : status, (int)response.StatusCode);
                Assert.Equal(1, Probe(app).Operations - beforeOperations);
                Assert.Equal(action is "retry" or "cancel" ? 1 : 0, Probe(app).OwnershipChecks - beforeChecks);
                Assert.Equal(action is "retry" or "cancel" ? "ownership,command" : "command", Assert.Single(response.Headers.GetValues("X-Resource-Trace")));
                Assert.Null(response.Headers.CacheControl);
                var contract = await Capture(response);
                if (legacy) { references[action] = contract; }
                else { Equal(references[action], contract); }

                if (code is not null)
                {
                    Problem(contract, code, code == "concurrency-conflict" ? "The item was changed by someone else. Reload and try again." : code == "repository-not-found" ? "The repository provider could not find the repository with the selected access." : "The request could not be completed.");
                }

                if (outcome == "ok" && action == "queue")
                {
                    Assert.Equal("/sample/connection-jobs/7", contract.Location);
                    using var body = JsonDocument.Parse(contract.Body);
                    Assert.Equal(7, body.RootElement.GetProperty("id").GetInt32());
                    Assert.False(body.RootElement.GetProperty("existing").GetBoolean());
                }
            }
        }
    }

    [Theory]
    [InlineData("retry")]
    [InlineData("cancel")]
    public async Task Ownership_mismatch_returns_explicit_nested_NotFound_before_mutation(string action)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Command(client, action, "ownership-mismatch");
            Assert.Equal(404, (int)response.StatusCode);
            Assert.Empty(await response.Content.ReadAsByteArrayAsync());
            Assert.Equal(1, Probe(app).OwnershipChecks);
            Assert.Equal(0, Probe(app).Operations);
            Assert.Equal("ownership", Assert.Single(response.Headers.GetValues("X-Resource-Trace")));
        }
    }

    [Theory]
    [InlineData("ok", 200)]
    [InlineData("forbidden", 404)]
    [InlineData("missing", 404)]
    [InlineData("validation", 400)]
    [InlineData("coded-validation", 400)]
    public async Task Workspace_and_report_policies_keep_distinct_privacy_payloads_and_file_contracts(string outcome, int status)
    {
        var references = new Dictionary<string, ResponseContract>();
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var kind in new[] { "workspace", "reports/html", "reports/pdf" })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/sample/" + kind + "?outcome=" + outcome);
                request.Headers.TryAddWithoutValidation("If-None-Match", "*");
                using var response = await client.SendAsync(request);
                Assert.Equal(outcome == "ok" && kind == "workspace" ? 304 : status, (int)response.StatusCode);
                Assert.True(response.Headers.CacheControl?.NoStore);
                Assert.Equal(kind == "workspace", response.Headers.Pragma.Any(value => value.Name == "no-cache"));
                Assert.Equal("authorize-read", Assert.Single(response.Headers.GetValues("X-Resource-Trace")));
                var contract = await Capture(response);
                if (legacy) { references[kind] = contract; }
                else { Equal(references[kind], contract); }

                if (outcome != "ok")
                {
                    Assert.Null(contract.ETag);
                    var code = status == 404 ? "resource-unavailable" : kind == "workspace" ? "invalid-request" : "report-unavailable";
                    var title = kind == "workspace" ? status == 404 ? "This resource is unavailable." : "Check the request parameters." : status == 404 ? "This report is unavailable." : "This report could not be opened.";
                    Problem(contract, code, title);
                }
                else if (kind == "workspace")
                {
                    Assert.Equal(ExpectedTag(), contract.ETag);
                    Assert.Empty(contract.Body);
                }
                else if (kind.EndsWith("html", StringComparison.Ordinal))
                {
                    Assert.Equal(new byte[] { 60, 112, 62, 79, 75, 60, 47, 112, 62 }, contract.Body);
                    Assert.Equal("text/html", contract.MediaType);
                    Assert.Null(contract.Disposition);
                }
                else
                {
                    Assert.Equal(new byte[] { 37, 80, 68, 70, 45, 83, 65, 77, 80, 76, 69 }, contract.Body);
                    Assert.Equal("application/pdf", contract.MediaType);
                    Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
                    Assert.Equal("report.pdf", response.Content.Headers.ContentDisposition?.FileNameStar);
                }
            }

            Assert.Equal(3, Probe(app).Reads);
        }
    }

    [Theory]
    [InlineData("strong", 304)]
    [InlineData("weak", 304)]
    [InlineData("wildcard", 304)]
    [InlineData("different", 200)]
    [InlineData("none", 200)]
    public async Task Authorized_read_precedes_exact_SHA256_and_conditional_comparison(string selection, int status)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/sample/workspace");
            if (selection != "none")
            {
                request.Headers.TryAddWithoutValidation("If-None-Match", selection == "strong" ? ExpectedTag() : selection == "weak" ? "W/" + ExpectedTag() : selection == "wildcard" ? "*" : "\"different\"");
            }

            using var response = await client.SendAsync(request);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal(ExpectedTag(), response.Headers.ETag?.ToString());
            Assert.Equal(1, Probe(app).Reads);
            if (status == 200)
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());
                Assert.Equal("Synthetic resource", json.RootElement.GetProperty("name").GetString());
            }
            else { Assert.Empty(await response.Content.ReadAsByteArrayAsync()); }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ETag_already_written_survives_a_later_header_failure_without_widening_the_policy(bool unknown)
    {
        foreach (var legacy in new[] { true, false })
        {
            var context = new DefaultHttpContext();
            context.Features.Set<IHttpRequestFeature>(new LaterHeaderFailure(unknown));
            var probe = new ResourceProbe();
            var operations = new ResourceOperations(probe, new ResourceTrace());

            async Task<object?> Invoke()
            {
                if (legacy) { return await LegacyResourceBridges.ReadAsync(context, operations, "ok", CancellationToken.None); }

                return await new WorkspacePolicyFilter().InvokeAsync(new DefaultEndpointFilterInvocationContext(context), async _ => await ResourceBridges.ReadAsync(context, operations, "ok", CancellationToken.None));
            }

            if (unknown) { await Assert.ThrowsAsync<UnexpectedResourceException>(Invoke); }
            else
            {
                var result = await Invoke();
                var problem = legacy ? Assert.IsType<ProblemHttpResult>(((INestedHttpResult)result!).Result) : Assert.IsType<ProblemHttpResult>(result);
                Assert.Equal(400, problem.StatusCode);
                Assert.Equal("Check the request parameters.", problem.ProblemDetails.Title);
            }

            Assert.Equal(1, probe.Reads);
            Assert.Equal(ExpectedTag(), context.Response.Headers.ETag.ToString());
        }
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("cancel")]
    public async Task Unknown_failures_and_cancellation_propagate_across_all_policies(string outcome)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var requests = new Func<Task<HttpResponseMessage>>[] { () => Command(client, "save", outcome), () => client.GetAsync("/sample/workspace?outcome=" + outcome), () => client.GetAsync("/sample/reports/html?outcome=" + outcome), () => client.GetAsync("/sample/reports/pdf?outcome=" + outcome) };
            foreach (var request in requests)
            {
                if (outcome == "unknown") { await Assert.ThrowsAsync<UnexpectedResourceException>(request); }
                else { await Assert.ThrowsAnyAsync<OperationCanceledException>(request); }
            }
        }
    }

    [Theory]
    [InlineData("guard", 403)]
    [InlineData("binding", 400)]
    [InlineData("unsupported", 415)]
    public async Task Earlier_rejection_and_native_binding_skip_application_operations(string scenario, int status)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/sample/connections");
            request.Content = new StringContent(scenario == "binding" ? "{broken" : scenario == "unsupported" ? "plain" : "{\"outcome\":\"ok\"}", Encoding.UTF8, scenario == "unsupported" ? "text/plain" : "application/json");
            if (scenario == "guard") { request.Headers.Add("X-Sample-Block", "true"); }

            using var response = await client.SendAsync(request);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal(0, Probe(app).Operations);
            Assert.Equal(0, Probe(app).OwnershipChecks);
            Assert.Equal(0, Probe(app).Reads);
            Assert.Null(response.Headers.CacheControl);
        }
    }

    [Theory]
    [InlineData("workspace")]
    [InlineData("reports/html")]
    [InlineData("reports/pdf")]
    public async Task An_earlier_guard_keeps_resource_rejections_free_of_handler_headers(string kind)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/sample/" + kind);
            request.Headers.Add("X-Sample-Block", "true");
            using var response = await client.SendAsync(request);
            Assert.Equal(403, (int)response.StatusCode);
            Assert.Null(response.Headers.CacheControl);
            Assert.Empty(response.Headers.Pragma);
            Assert.Null(response.Headers.ETag);
            Assert.False(response.Headers.Contains("X-Resource-Trace"));
            Assert.Equal(0, Probe(app).Reads);
        }
    }

    [Theory]
    [InlineData("concurrency")]
    [InlineData("provider")]
    public async Task Privacy_policies_do_not_acquire_repository_specific_catches(string outcome)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var kind in new[] { "workspace", "reports/html", "reports/pdf" })
            {
                if (outcome == "concurrency") { await Assert.ThrowsAsync<StaleResourceException>(() => client.GetAsync("/sample/" + kind + "?outcome=" + outcome)); }
                else { await Assert.ThrowsAsync<RemoteResourceMissingException>(() => client.GetAsync("/sample/" + kind + "?outcome=" + outcome)); }
            }
        }
    }

    [Fact]
    public async Task Native_response_metadata_and_API_Explorer_preserve_original_types_media_order_and_omissions()
    {
        await using var native = Create(true);
        await using var medium = Create(false);
        await native.StartAsync();
        await medium.StartAsync();
        Assert.Equal(Descriptions(native), Descriptions(medium));
        foreach (var item in new[] { ("/sample/connections", "GET"), ("/sample/connections", "POST"), ("/sample/connections/{resourceId:int}", "DELETE"), ("/sample/connection-jobs", "POST"), ("/sample/connection-jobs/{resourceId:int}/retry", "POST"), ("/sample/connection-jobs/{resourceId:int}/cancel", "POST"), ("/sample/workspace", "GET"), ("/sample/reports/html", "GET"), ("/sample/reports/pdf", "GET") })
        {
            var reference = Route(native, item.Item1, item.Item2);
            var actual = Route(medium, item.Item1, item.Item2);
            Assert.Equal(Responses(reference), Responses(actual));
            Assert.Equal(reference.Metadata.GetMetadata<MethodInfo>()!.ReturnType, actual.Metadata.GetMetadata<MethodInfo>()!.ReturnType);
            Assert.DoesNotContain(actual.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 409);
            if (item.Item1.Contains("workspace", StringComparison.Ordinal) || item.Item1.Contains("reports", StringComparison.Ordinal))
            {
                Assert.DoesNotContain(actual.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 403);
            }
        }

        var queue = Route(medium, "/sample/connection-jobs", "POST").Metadata.GetMetadata<MethodInfo>()!;
        Assert.Equal(typeof(Task<Results<Accepted<QueueReceipt>, ForbidHttpResult, NotFound, ProblemHttpResult>>), queue.ReturnType);
        var retry = Route(medium, "/sample/connection-jobs/{resourceId:int}/retry", "POST").Metadata.GetMetadata<MethodInfo>()!;
        Assert.Equal(typeof(Task<Results<Results<NoContent, NotFound>, ForbidHttpResult, NotFound, ProblemHttpResult>>), retry.ReturnType);
    }

    [Fact]
    public void Application_filters_have_no_response_or_fallback_declarations()
    {
        foreach (var type in new[] { typeof(RepositoryPolicyFilter), typeof(WorkspacePolicyFilter), typeof(ReportPolicyFilter) })
        {
            Assert.Empty(type.GetCustomAttributes<EndpointResponseAttribute>(false));
            Assert.Empty(type.GetCustomAttributes<EndpointResponseFallbackAttribute>(false));
        }
    }

    private static string ExpectedTag()
    {
        // Deliberately independent of either helper and its serializer call.
        return '"' + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("{\"Id\":7,\"Name\":\"Synthetic resource\"}"))) + '"';
    }

    private static async Task<ResponseContract> Capture(HttpResponseMessage response)
    {
        return new ResponseContract(await response.Content.ReadAsByteArrayAsync(), response.Content.Headers.ContentType?.MediaType, response.Headers.Location?.OriginalString, response.Headers.ETag?.ToString(), response.Content.Headers.ContentDisposition?.ToString());
    }

    private static void Equal(ResponseContract expected, ResponseContract actual)
    {
        Assert.Equal(expected.Body, actual.Body);
        Assert.Equal(expected.MediaType, actual.MediaType);
        Assert.Equal(expected.Location, actual.Location);
        Assert.Equal(expected.ETag, actual.ETag);
        Assert.Equal(expected.Disposition, actual.Disposition);
    }

    private static void Problem(ResponseContract contract, string code, string title)
    {
        using var json = JsonDocument.Parse(contract.Body);
        Assert.Equal("application/problem+json", contract.MediaType);
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        Assert.Equal(title, json.RootElement.GetProperty("title").GetString());
        Assert.DoesNotContain("Internal synthetic detail", Encoding.UTF8.GetString(contract.Body), StringComparison.Ordinal);
        Assert.DoesNotContain("private-code", Encoding.UTF8.GetString(contract.Body), StringComparison.Ordinal);
    }

    private sealed record ResponseContract(byte[] Body, string? MediaType, string? Location, string? ETag, string? Disposition);

    private sealed class LaterHeaderFailure(bool unknown) : IHttpRequestFeature
    {
        public string Protocol { get; set; } = "HTTP/1.1";
        public string Scheme { get; set; } = "http";
        public string Method { get; set; } = "GET";
        public string PathBase { get; set; } = "";
        public string Path { get; set; } = "/sample/workspace";
        public string QueryString { get; set; } = "";
        public string RawTarget { get; set; } = "/sample/workspace";
        public Stream Body { get; set; } = Stream.Null;

        public IHeaderDictionary Headers
        {
            get
            {
                if (unknown) { throw new UnexpectedResourceException(); }

                throw new InvalidResourceException("private-code");
            }
            set { }
        }
    }
}