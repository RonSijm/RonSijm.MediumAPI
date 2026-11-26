using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.ScopedCommandsExample.Api;
using Xunit;
using static RonSijm.MediumAPI.ScopedCommandsExample.IntegrationTest.ScopedCommandsHost;

namespace RonSijm.MediumAPI.ScopedCommandsExample.IntegrationTest;

public sealed class ScopedCommandTests
{
    [Theory]
    [InlineData("read", "allow", "ok", 200, 2, 1, 1, null)]
    [InlineData("read", "deny", "ok", 404, 2, 1, 0, null)]
    [InlineData("read", "forbidden", "ok", 403, 1, 1, 0, null)]
    [InlineData("read", "missing", "ok", 404, 1, 1, 0, null)]
    [InlineData("read", "validation", "ok", 400, 1, 1, 0, "invalid-command")]
    [InlineData("read", "allow", "forbidden", 403, 1, 1, 1, null)]
    [InlineData("read", "allow", "domain", 400, 1, 1, 1, "invalid-request")]
    [InlineData("read", "allow", "conflict", 409, 1, 1, 1, "command-conflict")]
    [InlineData("review", "allow", "ok", 204, 3, 1, 1, null)]
    [InlineData("review", "allow", "problem", 400, 3, 1, 1, "review-invalid")]
    [InlineData("review", "deny", "ok", 404, 2, 1, 0, null)]
    [InlineData("review", "allow", "missing", 404, 1, 1, 1, null)]
    [InlineData("release", "allow", "ok", 200, 1, 0, 1, null)]
    [InlineData("release", "allow", "confirmation", 409, 1, 0, 1, "confirmation-required")]
    [InlineData("release", "allow", "validation", 400, 1, 0, 1, "invalid-command")]
    [InlineData("release", "allow", "conflict", 409, 1, 0, 1, "command-conflict")]
    public async Task Extracted_filters_preserve_wire_results_nested_union_shapes_guard_order_and_safe_errors(string kind, string scope, string outcome, int status, int depth, int scopeCalls, int operationCalls, string? code)
    {
        string? nativeBody = null;
        string? nativeMedia = null;
        string? nativeTrace = null;
        string? nativeLeaf = null;
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, kind, scope, outcome);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
            Assert.Equal("no-cache", Assert.Single(response.Headers.Pragma).Name);
            Assert.Equal(depth.ToString(System.Globalization.CultureInfo.InvariantCulture), Assert.Single(response.Headers.GetValues("X-Result-Depth")));
            Assert.Equal(scopeCalls, Probe(app).ScopeCalls);
            Assert.Equal(operationCalls, Probe(app).OperationCalls);
            var body = await response.Content.ReadAsStringAsync();
            var media = response.Content.Headers.ContentType?.MediaType;
            var trace = Assert.Single(response.Headers.GetValues("X-Command-Trace"));
            var leaf = Assert.Single(response.Headers.GetValues("X-Result-Leaf"));
            if (legacy)
            {
                nativeBody = body;
                nativeMedia = media;
                nativeTrace = trace;
                nativeLeaf = leaf;
            }
            else
            {
                Assert.Equal(nativeBody, body);
                Assert.Equal(nativeMedia, media);
                Assert.Equal(nativeTrace, trace);
                Assert.Equal(nativeLeaf, leaf);
            }

            if (code is not null)
            {
                using var problem = JsonDocument.Parse(body);
                Assert.Equal(code, problem.RootElement.GetProperty("code").GetString());
                Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("title").GetString()));
                Assert.DoesNotContain("Internal synthetic detail", body, StringComparison.Ordinal);
                Assert.Equal("application/problem+json", media);
            }

            if (scopeCalls > 0 && operationCalls > 0)
            {
                Assert.True(trace.IndexOf("scope:exit", StringComparison.Ordinal) < trace.IndexOf("operation:enter", StringComparison.Ordinal));
            }

            if (kind == "release")
            {
                Assert.StartsWith("confirmation:enter,common:enter,", trace, StringComparison.Ordinal);
                Assert.EndsWith("common:exit,confirmation:exit", trace, StringComparison.Ordinal);
            }
        }
    }

    [Theory]
    [InlineData("unmatched", 404)]
    [InlineData("malformed", 400)]
    [InlineData("unsupported", 415)]
    public async Task Native_routing_and_body_binding_failures_skip_handler_headers_and_application_operations(string scenario, int expectedStatus)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = scenario == "unmatched"
                ? new HttpRequestMessage(HttpMethod.Get, "/commands/projects/not-an-int/resources/7")
                : new HttpRequestMessage(HttpMethod.Post, scenario == "malformed" ? "/commands/projects/3/resources/7/review" : "/commands/releases/7");
            if (scenario != "unmatched")
            {
                request.Content = new StringContent(scenario == "malformed" ? "{broken" : "plain", Encoding.UTF8, scenario == "malformed" ? "application/json" : "text/plain");
            }

            using var response = await client.SendAsync(request);
            Assert.Equal(expectedStatus, (int)response.StatusCode);
            Assert.Null(response.Headers.CacheControl);
            Assert.Empty(response.Headers.Pragma);
            Assert.Equal(0, Probe(app).ScopeCalls);
            Assert.Equal(0, Probe(app).OperationCalls);
        }
    }

    [Fact]
    public async Task Earlier_native_ingress_rejection_is_header_free_and_does_not_enter_application_scope_guards()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var kind in new[] { "read", "review", "release" })
            {
                using var response = await Send(client, kind, "allow", "ok", blocked: true);
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.Null(response.Headers.CacheControl);
                Assert.Empty(response.Headers.Pragma);
                Assert.False(response.Headers.Contains("X-Command-Trace"));
            }

            Assert.Equal(0, Probe(app).ScopeCalls);
            Assert.Equal(0, Probe(app).OperationCalls);
        }
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("cancel")]
    public async Task Unknown_exceptions_and_cancellation_pass_through_both_scope_and_specialized_command_policies(string outcome)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var item in new (string Kind, string Scope, string Operation, int ScopeCalls, int OperationCalls)[] { ("read", outcome, "ok", 1, 0), ("read", "allow", outcome, 1, 1), ("release", "allow", outcome, 0, 1) })
            {
                var beforeScope = Probe(app).ScopeCalls;
                var beforeOperation = Probe(app).OperationCalls;
                if (outcome == "cancel")
                {
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Send(client, item.Kind, item.Scope, item.Operation));
                }
                else
                {
                    await Assert.ThrowsAsync<UnexpectedCommandException>(() => Send(client, item.Kind, item.Scope, item.Operation));
                }

                Assert.Equal(item.ScopeCalls, Probe(app).ScopeCalls - beforeScope);
                Assert.Equal(item.OperationCalls, Probe(app).OperationCalls - beforeOperation);
            }
        }
    }

    [Fact]
    public async Task Real_API_Explorer_and_native_response_metadata_keep_the_original_nested_command_contracts()
    {
        await using var app = Create();
        await using var native = Create(legacy: true);
        await app.StartAsync();
        await native.StartAsync();
        Assert.Equal(3, Descriptions(app).Length);
        Assert.Equal(Descriptions(native), Descriptions(app));
        foreach (var path in new[] { "/commands/projects/{projectId:int}/resources/{resourceId:int}", "/commands/projects/{projectId:int}/resources/{resourceId:int}/review", "/commands/releases/{releaseId:int}" })
        {
            var declared = Route(app, path);
            var legacy = Route(native, path);
            Assert.Equal(NativeResponses(legacy), NativeResponses(declared));
            Assert.Equal(legacy.Metadata.GetMetadata<MethodInfo>()!.ReturnType, declared.Metadata.GetMetadata<MethodInfo>()!.ReturnType);
            Assert.Equal(legacy.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods, declared.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
            Assert.NotNull(declared.Metadata.GetMetadata<MediumApiEndpointMetadata>());
            Assert.Null(legacy.Metadata.GetMetadata<MediumApiEndpointMetadata>());
        }

        Assert.Equal(typeof(Task<Results<Results<Results<ProblemHttpResult, NoContent>, NotFound>, ForbidHttpResult, NotFound, ProblemHttpResult>>), typeof(ReviewScopedResourceEndpoint).GetMethod("HandleAsync")!.ReturnType);
        Assert.Equal(0, Probe(app).ScopeCalls);
        Assert.Equal(0, Probe(app).OperationCalls);
    }

    [Fact]
    public async Task Ownership_is_awaited_before_the_callback_in_the_retained_application_helper()
    {
        foreach (var legacy in new[] { true, false })
        {
            var pause = new OwnershipPause(enabled: true);
            await using var app = Create(legacy, pause);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var pending = Send(client, "read", "allow", "ok");
            try
            {
                await pause.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.Equal(1, Probe(app).ScopeCalls);
                Assert.Equal(0, Probe(app).OperationCalls);
                Assert.False(pending.IsCompleted);
                pause.Continue.TrySetResult(true);
                using var response = await pending;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(1, Probe(app).OperationCalls);
            }
            finally
            {
                pause.Continue.TrySetResult(true);
            }
        }
    }

    [Fact]
    public async Task Inherited_original_method_and_native_request_scope_binding_are_preserved()
    {
        await using var app = Create();
        await app.StartAsync();
        var endpoint = Route(app, "/commands/projects/{projectId:int}/resources/{resourceId:int}");
        Assert.Equal(typeof(ReadCommandHandler), endpoint.Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        Assert.Equal(typeof(ReadScopedResourceEndpoint), endpoint.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        using var client = app.GetTestClient();
        using var first = await Send(client, "read", "allow", "ok");
        using var second = await Send(client, "read", "allow", "ok");
        foreach (var response in new[] { first, second })
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(Assert.Single(response.Headers.GetValues("X-Service-Scope")), Assert.Single(response.Headers.GetValues("X-Filter-Scope")));
            Assert.Equal(new ResourceReceipt(7, "read"), await response.Content.ReadFromJsonAsync<ResourceReceipt>());
            Assert.True(response.Headers.CacheControl?.NoStore);
        }

        Assert.NotEqual(Assert.Single(first.Headers.GetValues("X-Service-Scope")), Assert.Single(second.Headers.GetValues("X-Service-Scope")));
    }
}