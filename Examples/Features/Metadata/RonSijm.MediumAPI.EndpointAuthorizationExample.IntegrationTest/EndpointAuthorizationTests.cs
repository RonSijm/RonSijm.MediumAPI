using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest.EndpointAuthorizationHost;

namespace RonSijm.MediumAPI.EndpointAuthorizationExample.IntegrationTest;

public sealed class EndpointAuthorizationTests
{
    [Fact]
    public async Task Class_declarations_match_independent_native_route_API_Explorer_and_authorization_inventories()
    {
        await using var native = Create(native: true);
        await using var generated = Create();
        await native.StartAsync();
        await generated.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.Equal(AuthorizationInventory(native), AuthorizationInventory(generated));
        Assert.Equal(8, Routes(generated).Count());
        Assert.Equal(8, Descriptions(generated).Operations.Count);
        Assert.Equal(0, Probe(native).TotalCalls);
        Assert.Equal(0, Probe(generated).TotalCalls);
    }

    [Fact]
    public async Task Effective_metadata_keeps_group_class_method_property_and_Configure_authorization_cumulative()
    {
        await using var app = Create();
        await app.StartAsync();
        Assert.Empty(Route(app, "/auth/open").Metadata.GetOrderedMetadata<IAuthorizeData>());
        Assert.Null(Assert.Single(Route(app, "/auth/default").Metadata.GetOrderedMetadata<IAuthorizeData>()).Policy);
        Assert.Equal(SamplePolicies.Readers, Assert.Single(Route(app, "/auth/read").Metadata.GetOrderedMetadata<IAuthorizeData>()).Policy);
        var roles = Assert.Single(Route(app, "/auth/roles").Metadata.GetOrderedMetadata<IAuthorizeData>());
        Assert.Equal("Reader,Administrator", roles.Roles);
        Assert.Equal(SamplePolicies.SecondaryScheme, roles.AuthenticationSchemes);
        Assert.Equal(new[] { SamplePolicies.GroupAccess, SamplePolicies.Readers, SamplePolicies.Writers }, Route(app, "/auth/protected/combined").Metadata.GetOrderedMetadata<IAuthorizeData>().Select(data => data.Policy));
        Assert.Equal(new[] { SamplePolicies.Readers, SamplePolicies.Writers, SamplePolicies.Audit }, Route(app, "/auth/property").Metadata.GetOrderedMetadata<IAuthorizeData>().Select(data => data.Policy));
        Assert.Equal(new[] { SamplePolicies.GroupAccess, SamplePolicies.Readers }, Route(app, "/auth/protected/public-class").Metadata.GetOrderedMetadata<IAuthorizeData>().Select(data => data.Policy));
        Assert.NotNull(Route(app, "/auth/protected/public-class").Metadata.GetMetadata<IAllowAnonymous>());
        Assert.NotNull(Route(app, "/auth/public-method").Metadata.GetMetadata<IAllowAnonymous>());
    }

    [Theory]
    [InlineData("/auth/default", "default", null, SamplePolicies.PrimaryScheme, HttpStatusCode.Unauthorized)]
    [InlineData("/auth/default", "default", "member", SamplePolicies.PrimaryScheme, HttpStatusCode.OK)]
    [InlineData("/auth/read", "read", null, SamplePolicies.PrimaryScheme, HttpStatusCode.Unauthorized)]
    [InlineData("/auth/read", "read", "member", SamplePolicies.PrimaryScheme, HttpStatusCode.Forbidden)]
    [InlineData("/auth/read", "read", "reader", SamplePolicies.PrimaryScheme, HttpStatusCode.OK)]
    [InlineData("/auth/roles", "roles", "reader", SamplePolicies.SecondaryScheme, HttpStatusCode.OK)]
    [InlineData("/auth/roles", "roles", "administrator", SamplePolicies.SecondaryScheme, HttpStatusCode.OK)]
    [InlineData("/auth/roles", "roles", "member", SamplePolicies.SecondaryScheme, HttpStatusCode.Forbidden)]
    [InlineData("/auth/roles", "roles", "reader", SamplePolicies.PrimaryScheme, HttpStatusCode.Unauthorized)]
    [InlineData("/auth/protected/combined", "combined", null, SamplePolicies.PrimaryScheme, HttpStatusCode.Unauthorized)]
    [InlineData("/auth/protected/combined", "combined", "catalog-reader", SamplePolicies.PrimaryScheme, HttpStatusCode.Forbidden)]
    [InlineData("/auth/protected/combined", "combined", "reader-writer", SamplePolicies.PrimaryScheme, HttpStatusCode.Forbidden)]
    [InlineData("/auth/protected/combined", "combined", "complete", SamplePolicies.PrimaryScheme, HttpStatusCode.OK)]
    [InlineData("/auth/property", "property", null, SamplePolicies.PrimaryScheme, HttpStatusCode.Unauthorized)]
    [InlineData("/auth/property", "property", "reader", SamplePolicies.PrimaryScheme, HttpStatusCode.Forbidden)]
    [InlineData("/auth/property", "property", "reader-writer", SamplePolicies.PrimaryScheme, HttpStatusCode.Forbidden)]
    [InlineData("/auth/property", "property", "complete", SamplePolicies.PrimaryScheme, HttpStatusCode.OK)]
    [InlineData("/auth/open", "open", null, SamplePolicies.PrimaryScheme, HttpStatusCode.OK)]
    [InlineData("/auth/public-method", "public-method", null, SamplePolicies.PrimaryScheme, HttpStatusCode.OK)]
    [InlineData("/auth/protected/public-class", "public-class", null, SamplePolicies.PrimaryScheme, HttpStatusCode.OK)]
    public async Task Native_cookie_authentication_preserves_challenge_forbid_allow_and_anonymous_behavior(string path, string handler, string? identity, string scheme, HttpStatusCode expectedStatus)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native, includeSignIn: true);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var cookie = identity is null ? null : await Issue(client, identity, scheme);
            using var response = await Send(client, path, cookie);
            Assert.Equal(expectedStatus, response.StatusCode);
            Assert.Null(response.Headers.Location);
            Assert.Equal(expectedStatus == HttpStatusCode.OK ? 1 : 0, Probe(app).TotalCalls);
            Assert.Equal(expectedStatus == HttpStatusCode.OK ? 1 : 0, Probe(app).CallsFor(handler));
            if (expectedStatus == HttpStatusCode.OK)
            {
                Assert.Equal(new AuthorizationReceipt(handler, identity is not null), await response.Content.ReadFromJsonAsync<AuthorizationReceipt>());
            }
        }
    }

    [Theory]
    [InlineData("/auth/open", "open", null, HttpStatusCode.Unauthorized)]
    [InlineData("/auth/open", "open", "member", HttpStatusCode.Forbidden)]
    [InlineData("/auth/open", "open", "complete", HttpStatusCode.OK)]
    [InlineData("/auth/default", "default", "member", HttpStatusCode.OK)]
    [InlineData("/auth/public-method", "public-method", null, HttpStatusCode.OK)]
    public async Task Host_fallback_policy_applies_to_unannotated_routes_while_class_Authorize_uses_DefaultPolicy(string path, string handler, string? identity, HttpStatusCode expectedStatus)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native, includeSignIn: true, configure: builder => builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireClaim("fallback", "allowed").Build()));
            await app.StartAsync();
            using var client = app.GetTestClient();
            var cookie = identity is null ? null : await Issue(client, identity);
            using var response = await Send(client, path, cookie);
            Assert.Equal(expectedStatus, response.StatusCode);
            Assert.Equal(expectedStatus == HttpStatusCode.OK ? 1 : 0, Probe(app).CallsFor(handler));
            if (expectedStatus == HttpStatusCode.OK)
            {
                Assert.Equal(new AuthorizationReceipt(handler, identity is not null), await response.Content.ReadFromJsonAsync<AuthorizationReceipt>());
            }
        }
    }

    [Fact]
    public async Task Invalid_cookie_values_are_rejected_by_native_authentication_before_the_handler()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Send(client, "/auth/default", SamplePolicies.PrimaryCookie + "=invalid-synthetic-cookie");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }

    [Fact]
    public async Task Separate_hosts_keep_native_cookie_keys_and_handler_probes_independent()
    {
        foreach (var native in new[] { true, false })
        {
            await using var first = Create(native, includeSignIn: true);
            await using var second = Create(native);
            await first.StartAsync();
            await second.StartAsync();
            using var firstClient = first.GetTestClient();
            using var secondClient = second.GetTestClient();
            var cookie = await Issue(firstClient, "reader");
            using var rejected = await Send(secondClient, "/auth/read", cookie);
            Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
            Assert.Equal(0, Probe(second).TotalCalls);
            Assert.Equal(new AuthorizationReceipt("read", true), await Read(firstClient, "/auth/read", cookie));
            Assert.Equal(1, Probe(first).TotalCalls);
        }
    }

    [Fact]
    public async Task Original_handler_parameters_and_typed_success_metadata_remain_native()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            foreach (var route in Routes(app))
            {
                var method = route.Metadata.GetMetadata<MethodInfo>()!;
                Assert.Equal(new[] { typeof(HttpContext), typeof(AuthorizationProbe) }, method.GetParameters().Select(parameter => parameter.ParameterType));
            }

            Assert.All(Descriptions(app).Operations, operation =>
            {
                Assert.Empty(operation.Parameters);
                Assert.Equal(200, Assert.Single(operation.Responses).StatusCode);
            });
        }
    }
}