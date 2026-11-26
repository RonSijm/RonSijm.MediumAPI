using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.ProviderPoliciesExample.Api;
using Xunit;
using static RonSijm.MediumAPI.ProviderPoliciesExample.IntegrationTest.ProviderPoliciesHost;

namespace RonSijm.MediumAPI.ProviderPoliciesExample.IntegrationTest;

public sealed class ProviderPolicyTests
{
    [Theory]
    [InlineData("remote", "ok", 200, null)]
    [InlineData("remote", "provider", 502, "remote-state-unconfirmed")]
    [InlineData("remote", "forbidden", 403, null)]
    [InlineData("remote", "missing", 404, null)]
    [InlineData("remote", "validation", 400, "invalid-operation")]
    [InlineData("local-typed", "ok", 200, null)]
    [InlineData("local-typed", "provider", 400, "local-provider-unavailable")]
    [InlineData("local-typed", "configuration", 400, "local-provider-unavailable")]
    [InlineData("local-typed", "validation", 400, "invalid-operation")]
    [InlineData("local-typed", "conflict", 409, "command-conflict")]
    [InlineData("local-dynamic", "ok", 200, null)]
    [InlineData("local-dynamic", "provider", 400, "local-provider-unavailable")]
    [InlineData("local-dynamic", "configuration", 400, "local-provider-unavailable")]
    [InlineData("local-dynamic", "forbidden", 403, null)]
    [InlineData("local-dynamic", "validation", 400, "invalid-operation")]
    [InlineData("local-dynamic", "conflict", 409, "command-conflict")]
    public async Task Three_provider_policies_keep_legacy_HTTP_contracts_catch_order_and_header_boundaries(string policy, string outcome, int expectedStatus, string? code)
    {
        string? legacyBody = null;
        string? legacyMedia = null;
        string? legacyTrace = null;
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, policy, outcome);
            Assert.Equal(expectedStatus, (int)response.StatusCode);
            Assert.Equal(policy == "local-typed", response.Headers.CacheControl?.NoStore == true);
            Assert.Equal("enabled", Assert.Single(response.Headers.GetValues("X-Policy-Group")));
            Assert.Equal(1, Probe(app).Calls);
            var body = await response.Content.ReadAsStringAsync();
            var media = response.Content.Headers.ContentType?.MediaType;
            var trace = Assert.Single(response.Headers.GetValues("X-Policy-Trace"));
            if (legacy)
            {
                legacyBody = body;
                legacyMedia = media;
                legacyTrace = trace;
            }
            else
            {
                Assert.Equal(legacyBody, body);
                Assert.Equal(legacyMedia, media);
                Assert.Equal(legacyTrace, trace);
            }

            if (code is not null)
            {
                using var problem = JsonDocument.Parse(body);
                Assert.Equal(code, problem.RootElement.GetProperty("code").GetString());
                Assert.Equal(Title(code), problem.RootElement.GetProperty("title").GetString());
                Assert.DoesNotContain("Internal synthetic detail", body, StringComparison.Ordinal);
                Assert.Equal("application/problem+json", media);
            }

            Assert.StartsWith(policy == "local-typed" ? "provider:enter,common:enter," : policy == "remote" ? "common:enter,remote:enter," : "common:enter,provider:enter,", trace, StringComparison.Ordinal);
            Assert.EndsWith(policy == "local-typed" ? "provider:exit" : "common:exit", trace, StringComparison.Ordinal);
            if (outcome is "provider" or "configuration")
            {
                var handled = policy == "remote" ? "remote:handled" : outcome == "provider" ? "provider:handled" : "provider:configuration";
                Assert.Equal(policy == "local-typed", trace.IndexOf("common:exit", StringComparison.Ordinal) < trace.IndexOf(handled, StringComparison.Ordinal));
            }
        }
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("cancel")]
    public async Task Unknown_failures_and_cancellation_escape_all_three_policies(string outcome)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var policy in new[] { "remote", "local-typed", "local-dynamic" })
            {
                var before = Probe(app).Calls;
                if (outcome == "cancel")
                {
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Send(client, policy, outcome));
                }
                else
                {
                    await Assert.ThrowsAsync<UnexpectedProviderException>(() => Send(client, policy, outcome));
                }

                Assert.Equal(1, Probe(app).Calls - before);
            }

            if (outcome == "unknown")
            {
                await Assert.ThrowsAsync<ProviderConfigurationException>(() => Send(client, "remote", "configuration"));
            }
        }
    }

    [Theory]
    [InlineData("outer", 403)]
    [InlineData("malformed", 400)]
    [InlineData("unsupported", 415)]
    public async Task Outer_rejection_and_native_body_binding_keep_group_header_timing_and_skip_handler_policies(string scenario, int expectedStatus)
    {
        foreach (var policy in new[] { "remote", "local-typed", "local-dynamic" })
        {
            bool? legacyGroupHeader = null;
            foreach (var legacy in new[] { true, false })
            {
                await using var app = Create(legacy);
                await app.StartAsync();
                using var client = app.GetTestClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, "/providers/" + policy + "/7");
                request.Content = new StringContent(scenario == "malformed" ? "{broken" : scenario == "unsupported" ? "plain" : "{\"outcome\":\"ok\"}", Encoding.UTF8, scenario == "unsupported" ? "text/plain" : "application/json");
                if (scenario == "outer")
                {
                    request.Headers.Add("X-Sample-Block", "true");
                }

                using var response = await client.SendAsync(request);
                Assert.Equal(expectedStatus, (int)response.StatusCode);
                Assert.Null(response.Headers.CacheControl);
                Assert.Equal(0, Probe(app).Calls);
                var hasGroupHeader = response.Headers.Contains("X-Policy-Group");
                if (legacy)
                {
                    legacyGroupHeader = hasGroupHeader;
                }
                else
                {
                    Assert.Equal(legacyGroupHeader, hasGroupHeader);
                }

                if (scenario == "outer")
                {
                    Assert.False(hasGroupHeader);
                    Assert.False(response.Headers.Contains("X-Policy-Trace"));
                }
            }
        }
    }

    [Fact]
    public async Task Native_API_Explorer_and_CLR_contracts_preserve_existing_documentation_including_omissions()
    {
        await using var app = Create();
        await using var legacy = Create(legacy: true);
        await app.StartAsync();
        await legacy.StartAsync();
        Assert.Equal(3, Descriptions(app).Length);
        Assert.Equal(Descriptions(legacy), Descriptions(app));
        foreach (var path in new[] { "/providers/remote/{providerId:int}", "/providers/local-typed/{providerId:int}", "/providers/local-dynamic/{providerId:int}" })
        {
            var endpoint = Route(app, path);
            var reference = Route(legacy, path);
            Assert.Equal(NativeResponses(reference), NativeResponses(endpoint));
            Assert.Equal(reference.Metadata.GetMetadata<MethodInfo>()!.ReturnType, endpoint.Metadata.GetMetadata<MethodInfo>()!.ReturnType);
            Assert.Equal(new[] { "POST" }, endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
            Assert.NotNull(endpoint.Metadata.GetMetadata<MediumApiEndpointMetadata>());
            Assert.Null(reference.Metadata.GetMetadata<MediumApiEndpointMetadata>());
        }

        Assert.Equal(typeof(Task<Results<Results<Ok<ProviderReceipt>, ProblemHttpResult>, ForbidHttpResult, NotFound, ProblemHttpResult>>), typeof(RemoteProviderEndpoint).GetMethod("HandleAsync")!.ReturnType);
        foreach (var path in new[] { "/providers/local-typed/{providerId:int}", "/providers/local-dynamic/{providerId:int}" })
        {
            Assert.DoesNotContain(Description(app, path).SupportedResponseTypes, response => response.StatusCode == 409 || response.StatusCode == 502);
        }

        Assert.Equal(typeof(void), Assert.Single(Route(app, "/providers/remote/{providerId:int}").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 502).Type);
        Assert.Equal(0, Probe(app).Calls);
    }

    [Fact]
    public async Task Static_command_mapper_calls_do_not_import_the_typed_filters_response_attributes()
    {
        await using var app = Create();
        app.MapGet("/metadata/static", BareDynamic).AddMediumApiFilter<LocalDynamicProviderPolicyFilter>();
        app.MapGet("/metadata/typed", BareDynamic).AddMediumApiFilter<TypedCommandPolicyFilter>();
        app.MapGet("/metadata/local-typed", BareDynamic).AddMediumApiFilter<LocalTypedProviderPolicyFilter>();
        await app.StartAsync();
        Assert.DoesNotContain(Route(app, "/metadata/static").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 404);
        Assert.DoesNotContain(Route(app, "/metadata/local-typed").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 404);
        Assert.Single(Route(app, "/metadata/typed").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 404);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/metadata/static");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, Probe(app).Calls);
    }

    [Fact]
    public async Task Dynamic_anonymous_response_remains_a_single_opened_property_without_a_new_schema_contract()
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = Create(legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, "local-dynamic", "ok");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(new[] { "opened" }, payload.RootElement.EnumerateObject().Select(property => property.Name));
            Assert.True(payload.RootElement.GetProperty("opened").GetBoolean());
            Assert.Equal(typeof(Task<IResult>), Route(app, "/providers/local-dynamic/{providerId:int}").Metadata.GetMetadata<MethodInfo>()!.ReturnType);
            Assert.Equal(typeof(void), Assert.Single(Description(app, "/providers/local-dynamic/{providerId:int}").SupportedResponseTypes, item => item.StatusCode == 200).Type);
        }
    }

    [Fact]
    public async Task Inherited_local_handler_metadata_and_native_request_services_remain_intact()
    {
        await using var app = Create();
        await app.StartAsync();
        var endpoint = Route(app, "/providers/local-typed/{providerId:int}");
        Assert.Equal(typeof(LocalTypedProviderHandler), endpoint.Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        Assert.Equal(typeof(LocalTypedProviderEndpoint), endpoint.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        using var client = app.GetTestClient();
        using var first = await Send(client, "local-typed", "ok");
        using var second = await Send(client, "local-typed", "ok");
        foreach (var response in new[] { first, second })
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new ProviderReceipt(7, "local-ready"), await response.Content.ReadFromJsonAsync<ProviderReceipt>());
            Assert.Equal(Assert.Single(response.Headers.GetValues("X-Service-Scope")), Assert.Single(response.Headers.GetValues("X-Filter-Scope")));
            Assert.True(response.Headers.CacheControl?.NoStore);
        }

        Assert.NotEqual(Assert.Single(first.Headers.GetValues("X-Service-Scope")), Assert.Single(second.Headers.GetValues("X-Service-Scope")));
    }

    private static IResult BareDynamic()
    {
        return Results.Ok(new { Opened = true });
    }

    private static string Title(string code)
    {
        return code switch
        {
            "remote-state-unconfirmed" => "Remote state could not be confirmed.",
            "local-provider-unavailable" => "Local provider tools are unavailable.",
            "invalid-operation" => "The request could not be completed.",
            "command-conflict" => "The item changed. Reload and try again.",
            _ => throw new InvalidOperationException("Unexpected synthetic error code.")
        };
    }
}