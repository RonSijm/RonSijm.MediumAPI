using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RonSijm.MediumAPI.Example.Endpoints;
using RonSijm.MediumAPI.Example.NamespaceApi;
using RonSijm.MediumAPI.Example.NamespaceApi.Child;
using Xunit;

namespace RonSijm.MediumAPI.Example.IntegrationTest;

public sealed class NamespaceGroupTests
{
    [Fact]
    public void Legacy_generated_registration_remains_binary_compatible()
    {
        Assert.Contains(typeof(EndpointRegistry).GetMethods(), method => method.Name == "Register"
                                                                         && method.IsGenericMethodDefinition && method.GetParameters().Length == 2
                                                                         && method.GetParameters()[1].HasDefaultValue);
    }

    [Theory]
    [InlineData("/namespace-api/value", "api")]
    [InlineData("/namespace-api/inherited", "inherited")]
    public async Task Namespace_and_descendants_use_the_nearest_definition(string path, string value)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(value, await response.Content.ReadAsStringAsync());
        Assert.Equal("api", Assert.Single(response.Headers.GetValues("X-Namespace")));
    }

    [Fact]
    public async Task Nested_definitions_compose_prefixes_parameters_and_filter_order()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/namespace-api/children/7/42?parentId=99&id=98");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(7, body.RootElement.GetProperty("parentId").GetInt32());
        Assert.Equal(42, body.RootElement.GetProperty("id").GetInt32());
        Assert.Equal(new[] { "api", "child" }, response.Headers.GetValues("X-Namespace"));
        Assert.Equal("/namespace-api/children/7/42", factory.Services.GetRequiredService<LinkGenerator>()
            .GetPathByName("NamespaceChild", new { parentId = 7, id = 42 }));
        var endpoint = Assert.Single(factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>(), endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "NamespaceChild");
        Assert.Equal("Namespace API", Assert.Single(endpoint.Metadata.GetMetadata<ITagsMetadata>()!.Tags));
    }

    [Theory]
    [InlineData("/namespace-api/unrelated")]
    [InlineData("/unrelated")]
    [InlineData("/namespace-api/explicit")]
    [InlineData("/children/7/42")]
    public async Task Explicit_namespace_mapping_does_not_include_unrelated_or_manual_endpoints(string path)
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Explicit_child_mount_skips_ancestors_and_hosts_do_not_share_builders()
    {
        await using var first = CreateHost();
        first.MapGroup("/one").MapMediumApiNamespace<ChildGroupDefinition>();
        await first.StartAsync();
        await using var second = CreateHost();
        second.MapGroup("/two").MapMediumApiNamespace<ChildGroupDefinition>();
        await second.StartAsync();
        using var firstClient = first.GetTestClient();
        using var secondClient = second.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await firstClient.GetAsync("/one/children/1/2")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await firstClient.GetAsync("/two/children/1/2")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await secondClient.GetAsync("/two/children/1/2")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await secondClient.GetAsync("/one/children/1/2")).StatusCode);
    }

    [Fact]
    public async Task Automatic_mapping_uses_namespace_definitions_once_and_preserves_root_endpoints()
    {
        await using var app = CreateHost();
        app.MapMediumApiEndpoints();
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/namespace-api/value")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/unrelated")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/namespace-api/explicit")).StatusCode);
        var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
        Assert.Single(routes, route => route.RoutePattern.RawText == "/namespace-api/value");
    }

    [Fact]
    public async Task Startup_conventions_and_manual_aliases_remain_supported()
    {
        await using var app = CreateHost();
        var api = app.MapMediumApiNamespace<ApiGroupDefinition>(group => group.WithMetadata(new StartupMarker()));
        api.MapMediumApiEndpoint<ExplicitValueEndpoint>("/alias").WithName("NamespaceExplicitAlias");
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/namespace-api/alias")).StatusCode);
        var endpoint = Assert.Single(((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>(), endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "NamespaceExplicitAlias");
        Assert.NotNull(endpoint.Metadata.GetMetadata<StartupMarker>());
    }

    [Theory]
    [InlineData("/namespace-api/value")]
    [InlineData("/namespace-api/children/1/2")]
    public async Task Group_authorization_is_enforced_for_direct_and_nested_endpoints(string path)
    {
        await using var app = CreateHost();
        app.MapMediumApiNamespace<ApiGroupDefinition>(group => group.RequireAuthorization());
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-User", "synthetic");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Namespace_selection_is_scoped_to_the_definition_assembly()
    {
        EndpointRegistry.Register<ForeignAssemblyEndpoint>((builder, _) => builder.MapGet("/foreign-assembly", () => "foreign"));
        await using var app = CreateHost();
        app.MapMediumApiNamespace<ApiGroupDefinition>();
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/namespace-api/foreign-assembly")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/foreign-assembly")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/namespace-api/value")).StatusCode);
    }

    private static WebApplication CreateHost()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthorization();
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        builder.Services.AddSingleton<EndpointActivationProbe>();
        builder.Services.AddScoped<ScopedEndpointValue>();
        return builder.Build();
    }

    private sealed class StartupMarker;

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            => Task.FromResult(Request.Headers.ContainsKey("X-Test-User")
                ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(
                    new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "synthetic") }, "Test")), "Test"))
                : AuthenticateResult.NoResult());
    }
}