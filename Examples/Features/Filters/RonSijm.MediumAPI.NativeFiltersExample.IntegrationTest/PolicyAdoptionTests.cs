using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.NativeFiltersExample.Program;

namespace RonSijm.MediumAPI.NativeFiltersExample.IntegrationTest;

public sealed class PolicyAdoptionTests
{
    private static readonly string[] DiagnosticRoutes = ["/diagnostics/value/7", "/diagnostics/native/7", "/native-reference/diagnostics/value/7"];

    [Theory]
    [InlineData(null)]
    [InlineData("203.0.113.24")]
    public async Task Missing_or_nonloopback_connections_reject_before_group_headers_and_downstream_filters(string? remoteAddress)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in DiagnosticRoutes)
        {
            using var response = await Send(client, path, remoteAddress);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(new DiagnosticError("loopback-required"), await response.Content.ReadFromJsonAsync<DiagnosticError>());
            AssertNoStore(response, expected: false);
            Assert.False(response.Headers.Contains("X-Observed-Handler"));
            Assert.Equal(new[] { "guard:enter", "guard:reject", "guard:exit" }, ReadTrace(response));
        }
    }

    [Theory]
    [InlineData("127.0.0.1", "ok", 200)]
    [InlineData("::1", "ok", 200)]
    [InlineData("127.0.0.1", "reject", 400)]
    [InlineData("127.0.0.1", "throw", 500)]
    public async Task Loopback_success_downstream_rejection_and_unexpected_failure_keep_headers_and_native_unwinding(string remoteAddress, string outcome, int expectedStatus)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in DiagnosticRoutes)
        {
            using var response = await Send(client, path + "?outcome=" + outcome + "&value=99", remoteAddress);
            Assert.Equal(expectedStatus, (int)response.StatusCode);
            AssertNoStore(response, expected: true);
            Assert.Equal("HandleAsync", Assert.Single(response.Headers.GetValues("X-Observed-Handler")));
            Assert.Equal("7", Assert.Single(response.Headers.GetValues("X-Observed-Value")));
            var expectedTrace = new[] { "guard:enter", "outcome:enter", outcome == "reject" ? "outcome:reject" : "handler", "outcome:exit", "guard:exit" };
            Assert.Equal(expectedTrace, ReadTrace(response));
            if (outcome == "ok")
            {
                var payload = await response.Content.ReadFromJsonAsync<DiagnosticReceipt>();
                Assert.NotNull(payload);
                Assert.Equal(7, payload.Value);
                Assert.Equal(expectedTrace, payload.Trace);
            }
            else
            {
                Assert.Equal(new DiagnosticError(outcome == "reject" ? "downstream-rejected" : "unexpected"), await response.Content.ReadFromJsonAsync<DiagnosticError>());
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_namespace_session_guard_remains_outside_handler_declared_headers(bool stale)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/session-sample/value/7", "/native-reference/session/value/7" })
        {
            using var response = await Send(client, path, "127.0.0.1", staleSession: stale);
            Assert.Equal(stale ? HttpStatusCode.Forbidden : HttpStatusCode.OK, response.StatusCode);
            AssertNoStore(response, expected: !stale);
            Assert.Equal(stale ? new[] { "session:enter", "session:reject", "session:exit" } : new[] { "session:enter", "handler", "session:exit" }, ReadTrace(response));
            if (stale)
            {
                Assert.Equal(new DiagnosticError("stale-session"), await response.Content.ReadFromJsonAsync<DiagnosticError>());
            }
            else
            {
                var payload = await response.Content.ReadFromJsonAsync<DiagnosticReceipt>();
                Assert.NotNull(payload);
                Assert.Equal(7, payload.Value);
                Assert.Equal(ReadTrace(response), payload.Trace);
            }
        }
    }

    [Fact]
    public async Task Original_handler_typed_responses_and_mapping_identity_keep_their_native_boundaries()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        var declared = Endpoint(app, "/diagnostics/value/{value:int}");
        var ordinary = Endpoint(app, "/diagnostics/native/{value:int}");
        var reference = Endpoint(app, "/native-reference/diagnostics/value/{value:int}");
        var identity = Assert.IsType<MediumApiEndpointMetadata>(declared.Metadata.GetMetadata<MediumApiEndpointMetadata>());
        Assert.Equal("RonSijm.MediumAPI.NativeFiltersExample.Diagnostics.GetDiagnosticValueEndpoint", identity.EndpointType.FullName);
        Assert.Equal(identity.EndpointType.GetMethod("HandleAsync"), declared.Metadata.GetMetadata<MethodInfo>());
        Assert.Equal(declared.Metadata.GetMetadata<MethodInfo>(), reference.Metadata.GetMetadata<MethodInfo>());
        Assert.Equal(declared.Metadata.GetMetadata<MethodInfo>(), ordinary.Metadata.GetMetadata<MethodInfo>());
        Assert.Null(ordinary.Metadata.GetMetadata<MediumApiEndpointMetadata>());
        Assert.Null(reference.Metadata.GetMetadata<MediumApiEndpointMetadata>());
        Assert.Equal("GetDiagnosticValue", declared.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
        Assert.Null(reference.Metadata.GetMetadata<IEndpointNameMetadata>());
        foreach (var endpoint in new[] { declared, ordinary, reference })
        {
            Assert.Equal(new[] { "GET" }, endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
            Assert.Contains(endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), metadata => metadata.StatusCode == 200 && metadata.Type == typeof(DiagnosticReceipt));
            Assert.Equal(ResponseContracts(declared), ResponseContracts(endpoint));
        }
    }

    [Fact]
    public async Task Native_class_authorization_rate_limit_and_tag_contracts_match_explicit_fluent_policies()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        var declared = Endpoint(app, "/policy-sample/value/{value:int}");
        var reference = Endpoint(app, "/native-reference/policy/value/{value:int}");
        foreach (var endpoint in new[] { declared, reference })
        {
            var authorization = Assert.Single(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>());
            Assert.IsType<AuthorizeAttribute>(authorization);
            Assert.Equal(DiagnosticPolicies.Readers, authorization.Policy);
            Assert.Null(authorization.Roles);
            Assert.Null(authorization.AuthenticationSchemes);
            Assert.Equal(DiagnosticPolicies.Burst, Assert.Single(endpoint.Metadata.GetOrderedMetadata<EnableRateLimitingAttribute>()).PolicyName);
            Assert.Equal(new[] { DiagnosticPolicies.Tag }, endpoint.Metadata.GetMetadata<ITagsMetadata>()!.Tags);
        }

        Assert.Equal(declared.Metadata.GetMetadata<MethodInfo>(), reference.Metadata.GetMetadata<MethodInfo>());
        Assert.NotNull(declared.Metadata.GetMetadata<MediumApiEndpointMetadata>());
        Assert.Null(reference.Metadata.GetMetadata<MediumApiEndpointMetadata>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Host_authorization_stays_before_the_plain_class_or_native_fluent_filter(bool authenticated)
    {
        await using var app = CreateHost();
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/policy-sample/value/7", "/native-reference/policy/value/7" })
        {
            using var response = await Send(client, path, "127.0.0.1", authenticated: authenticated);
            Assert.Equal(authenticated ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(authenticated ? new[] { "policy:enter", "handler", "policy:exit" } : Array.Empty<string>(), ReadTrace(response));
            AssertNoStore(response, expected: false);
            if (authenticated)
            {
                var payload = await response.Content.ReadFromJsonAsync<DiagnosticReceipt>();
                Assert.NotNull(payload);
                Assert.Equal(7, payload.Value);
                Assert.Equal(ReadTrace(response), payload.Trace);
            }
        }
    }

    private static WebApplication CreateHost()
    {
        return ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Services.AddSingleton<IStartupFilter, SyntheticConnectionStartupFilter>();
        });
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, string path, string? remoteAddress, bool staleSession = false, bool authenticated = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Sample-Remote", remoteAddress ?? "missing");
        if (staleSession)
        {
            request.Headers.Add("X-Sample-Session", "stale");
        }

        if (authenticated)
        {
            request.Headers.Add("X-Sample-Member", "true");
        }

        return await client.SendAsync(request);
    }

    private static void AssertNoStore(HttpResponseMessage response, bool expected)
    {
        Assert.Equal(expected, response.Headers.CacheControl?.NoStore == true);
        if (expected)
        {
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            Assert.Equal("no-cache", Assert.Single(response.Headers.Pragma).Name);
        }
        else
        {
            Assert.Null(response.Headers.CacheControl);
            Assert.Empty(response.Headers.Pragma);
        }
    }

    private static string[] ReadTrace(HttpResponseMessage response)
    {
        var value = Assert.Single(response.Headers.GetValues("X-Sample-Trace"));
        return value == "none" ? [] : value.Split(',');
    }

    private static string[] ResponseContracts(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()
            .Select(metadata => metadata.StatusCode + ":" + metadata.Type?.FullName + ":" + string.Join(",", metadata.ContentTypes))
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private static RouteEndpoint Endpoint(WebApplication app, string pattern)
    {
        return Assert.Single(app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>(), endpoint => endpoint.RoutePattern.RawText == pattern);
    }

    public sealed class SyntheticConnectionStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (context, invokeNext) =>
                {
                    var remote = context.Request.Headers["X-Sample-Remote"].ToString();
                    context.Connection.RemoteIpAddress = remote == "missing" || remote.Length == 0 ? null : IPAddress.Parse(remote);
                    if (context.Request.Headers["X-Sample-Member"] == "true")
                    {
                        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "synthetic-member")], "Synthetic"));
                    }

                    var trace = context.RequestServices.GetRequiredService<RequestTrace>();
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers["X-Sample-Trace"] = trace.Entries.Count == 0 ? "none" : string.Join(",", trace.Entries);
                        return Task.CompletedTask;
                    });
                    await invokeNext(context);
                });
                next(app);
            };
        }
    }
}