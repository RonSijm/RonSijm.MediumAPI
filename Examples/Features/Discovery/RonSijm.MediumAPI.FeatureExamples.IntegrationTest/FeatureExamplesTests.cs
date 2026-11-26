using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.FeatureExamples.Endpoints.Api;
using RonSijm.MediumAPI.FeatureExamples.Endpoints.Api.Development;
using RonSijm.MediumAPI.FeatureExamples.Endpoints.Api.Development.Child;
using Xunit;
using FeatureProgram = RonSijm.MediumAPI.FeatureExamples.Program;
using ScaffoldingProgram = RonSijm.MediumAPI.ScaffoldingExample.Program;

namespace RonSijm.MediumAPI.FeatureExamples.IntegrationTest;

public sealed class FeatureExamplesTests
{
    static FeatureExamplesTests()
    {
        // This test assembly is deliberately not in the application's profile.
        EndpointRegistry.Register<ForeignEndpoint>((builder, _) => builder.MapGet("/foreign", () => "foreign"));
    }

    [Fact]
    public void Profile_captures_a_deduplicated_mutation_safe_selection()
    {
        MediumApiDiscoveryOptions? retained = null;
        var configurationCalls = 0;
        var profile = MediumApiDiscoveryProfile.Create(options =>
        {
            configurationCalls++;
            retained = options;
            options.FromAssemblyContaining<FeatureProgram>().FromAssembly(typeof(FeatureProgram).Assembly);
        });
        retained!.FromAssemblyContaining<FeatureExamplesTests>();
        Assert.Equal(1, configurationCalls);
        Assert.Equal(typeof(FeatureProgram).Assembly, Assert.Single(profile.Assemblies));
        Assert.False(profile.Assemblies is Assembly[]);
        var exposedSelection = Assert.IsAssignableFrom<IList<Assembly>>(profile.Assemblies);
        Assert.True(exposedSelection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => exposedSelection[0] = typeof(FeatureExamplesTests).Assembly);
        Assert.Equal(typeof(FeatureProgram).Assembly, Assert.Single(EndpointRegistry.GetManifest(profile).Assemblies));
    }

    [Fact]
    public void Profiles_reject_null_configuration_and_empty_selection()
    {
        Assert.Throws<ArgumentNullException>(() => MediumApiDiscoveryProfile.Create(null!));
        Assert.Throws<InvalidOperationException>(() => MediumApiDiscoveryProfile.Create(_ => { }));
    }

    [Fact]
    public void Manifest_uses_the_same_profile_without_evaluating_host_conditions()
    {
        // There is no service provider here. Executing ShouldMap would require one.
        var manifest = EndpointRegistry.GetManifest(FeatureProgram.EndpointDiscovery);
        Assert.Single(manifest.Assemblies);
        Assert.Equal(7, manifest.Endpoints.Count);
        Assert.Equal(3, manifest.NamespaceGroups.Count);
        Assert.All(manifest.Endpoints, endpoint => Assert.Equal(typeof(FeatureProgram).Assembly, endpoint.Assembly));
        var product = Assert.Single(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(GetProductEndpoint));
        Assert.Equal("/products/{productId:int}", product.Pattern);
        Assert.Equal("GET", product.HttpMethod);
        Assert.Equal(typeof(ApiGroupDefinition), product.NamespaceGroup);
        Assert.True(product.MapAutomatically);
        var manual = Assert.Single(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(ManualAliasEndpoint));
        Assert.True(manual.IsManuallyMapped);
        var child = Assert.Single(manifest.NamespaceGroups, definition => definition.GroupType == typeof(ChildGroupDefinition));
        Assert.Equal(typeof(DevelopmentGroupDefinition), child.ParentGroup);
    }

    [Fact]
    public async Task Application_maps_actual_paths_named_links_and_manual_aliases()
    {
        await using var app = CreateHost();
        var probe = app.Services.GetRequiredService<FeatureMappingProbe>();
        Assert.Equal(0, probe.EndpointActivations);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(new ProductResponse(42, "Sample product"), await client.GetFromJsonAsync<ProductResponse>("/api/products/42"));
        Assert.Equal(1, probe.EndpointActivations);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/products/0")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/status")).StatusCode);
        Assert.Equal("manual", await client.GetFromJsonAsync<string>("/exports/current"));
        foreach (var absent in new[] { "/products/42", "/api/manual", "/manual", "/foreign", "/api/foreign" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(absent)).StatusCode);
        }

        var links = app.Services.GetRequiredService<LinkGenerator>();
        Assert.Equal("/api/products/42", links.GetPathByName("FeatureProduct", new { productId = 42 }));
        Assert.Equal("/exports/current", links.GetPathByName("FeatureManualAlias", new { }));
        var report = app.GetMediumApiRouteReport();
        Assert.False(report.HasConflicts);
        Assert.Single(report.Routes, route => route.Pattern == "/api/products/{productId:int}");
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Development", false)]
    public async Task Disabled_parent_suppresses_native_and_descendant_routes(string environment, bool enabled)
    {
        await using var app = CreateHost(environment, enabled);
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var absent in new[] { "/api/development/value", "/api/development/health", "/api/development/child/value" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(absent)).StatusCode);
        }

        var probe = app.Services.GetRequiredService<FeatureMappingProbe>();
        Assert.Equal(1, probe.DevelopmentConditions);
        Assert.Equal(0, probe.DevelopmentMaps);
        Assert.Equal(0, probe.ChildConditions);
        Assert.Equal(0, probe.ChildMaps);
    }

    [Fact]
    public async Task Shared_profile_evaluates_conditions_independently_for_each_host()
    {
        await using var enabled = CreateHost(enabled: true);
        await using var disabled = CreateHost(enabled: false);
        await enabled.StartAsync();
        await disabled.StartAsync();
        using var enabledClient = enabled.GetTestClient();
        using var disabledClient = disabled.GetTestClient();
        foreach (var path in new[] { "/api/development/value", "/api/development/health", "/api/development/child/value" })
        {
            Assert.Equal(HttpStatusCode.OK, (await enabledClient.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await disabledClient.GetAsync(path)).StatusCode);
        }

        var enabledProbe = enabled.Services.GetRequiredService<FeatureMappingProbe>();
        Assert.Equal((1, 1, 1, 1), (enabledProbe.DevelopmentConditions, enabledProbe.DevelopmentMaps, enabledProbe.ChildConditions, enabledProbe.ChildMaps));
        Assert.Equal(0, disabled.Services.GetRequiredService<FeatureMappingProbe>().DevelopmentMaps);
    }

    [Fact]
    public async Task Command_line_options_can_enable_the_development_example()
    {
        await using var app = FeatureProgram.Build(["--environment", "Development", "--Features:Development", "true"], configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/development/value")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/development/health")).StatusCode);
    }

    [Fact]
    public async Task Child_condition_can_suppress_only_the_child()
    {
        await using var app = CreateHost(enabled: true, childEnabled: false);
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/development/value")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/development/health")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/development/child/value")).StatusCode);
        Assert.Equal(0, app.Services.GetRequiredService<FeatureMappingProbe>().ChildMaps);
    }

    [Fact]
    public async Task Attribute_response_metadata_matches_fluent_framework_conventions()
    {
        await using var app = CreateHost();
        app.MapGet("/fluent/status", () => Results.NoContent()).WithName("FluentStatus").Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status400BadRequest);
        app.MapGet("/fluent/json", () => Results.Ok(new ProductResponse(1, "sample"))).WithName("FluentJson").Produces<ProductResponse>(StatusCodes.Status200OK);
        app.MapGet("/fluent/problem", () => Results.Problem(statusCode: 400)).WithName("FluentProblem").ProducesProblem(StatusCodes.Status400BadRequest);
        app.MapGet("/fluent/formats", () => Results.Text("plain", "text/plain")).WithName("FluentFormats").Produces<string>(StatusCodes.Status200OK, "text/plain", "application/json");
        await app.StartAsync();
        AssertSameResponse(app, "/api/status", "/fluent/status", StatusCodes.Status204NoContent);
        AssertSameResponse(app, "/api/status", "/fluent/status", StatusCodes.Status400BadRequest);
        AssertSameResponse(app, "/api/products/{productId:int}", "/fluent/json", StatusCodes.Status200OK);
        AssertSameResponse(app, "/api/problem", "/fluent/problem", StatusCodes.Status400BadRequest);
        AssertSameResponse(app, "/api/formats", "/fluent/formats", StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Api_explorer_and_actual_responses_retain_status_types_and_media()
    {
        await using var app = CreateHost();
        await app.StartAsync();
        var descriptions = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items).ToArray();
        var product = FindDescription(descriptions, "FeatureProduct");
        Assert.Contains(product.SupportedResponseTypes, response => response.StatusCode == 200 && response.Type == typeof(ProductResponse));
        Assert.Contains(product.SupportedResponseTypes, response => response.StatusCode == 404);
        var problem = FindDescription(descriptions, "FeatureProblem");
        Assert.Contains(problem.SupportedResponseTypes, response => response.StatusCode == 400 && response.Type == typeof(ProblemDetails));
        var formats = FindDescription(descriptions, "FeatureFormats");
        var responseType = Assert.Single(formats.SupportedResponseTypes, response => response.StatusCode == 200);
        Assert.Equal(new[] { "application/json", "text/plain" }, responseType.ApiResponseFormats.Select(format => format.MediaType).Order(StringComparer.Ordinal));
        using var client = app.GetTestClient();
        using var problemResponse = await client.GetAsync("/api/problem");
        Assert.Equal(HttpStatusCode.BadRequest, problemResponse.StatusCode);
        Assert.Equal("application/problem+json", problemResponse.Content.Headers.ContentType?.MediaType);
        using var textResponse = await client.GetAsync("/api/formats");
        Assert.Equal("text/plain", textResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal("plain", await textResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Scaffolding_sample_runs_with_its_declared_group_prefix()
    {
        await using var app = ScaffoldingProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await app.StartAsync();
        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/products")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/products")).StatusCode);
    }

    private static WebApplication CreateHost(string environment = "Development", bool enabled = false, bool childEnabled = true)
    {
        return FeatureProgram.Build(environment: environment, configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:Development"] = enabled.ToString(),
                ["Features:Child"] = childEnabled.ToString()
            });
        });
    }

    private static void AssertSameResponse(WebApplication app, string attributedPattern, string fluentPattern, int status)
    {
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().ToArray();
        var attributedEndpoint = Assert.Single(endpoints, endpoint => endpoint.RoutePattern.RawText == attributedPattern);
        var fluentEndpoint = Assert.Single(endpoints, endpoint => endpoint.RoutePattern.RawText == fluentPattern);
        var attributed = attributedEndpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Last(metadata => metadata.StatusCode == status);
        var fluent = fluentEndpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Last(metadata => metadata.StatusCode == status);
        Assert.Equal(fluent.StatusCode, attributed.StatusCode);
        Assert.Equal(fluent.Type, attributed.Type);
        Assert.Equal(fluent.ContentTypes, attributed.ContentTypes);
        var descriptions = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items).ToArray();
        var attributedDescription = FindDescription(descriptions, attributedEndpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName!);
        var fluentDescription = FindDescription(descriptions, fluentEndpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName!);
        var attributedResponse = Assert.Single(attributedDescription.SupportedResponseTypes, response => response.StatusCode == status);
        var fluentResponse = Assert.Single(fluentDescription.SupportedResponseTypes, response => response.StatusCode == status);
        Assert.Equal(fluentResponse.Type, attributedResponse.Type);
        Assert.Equal(fluentResponse.ApiResponseFormats.Select(format => format.MediaType).Order(StringComparer.Ordinal), attributedResponse.ApiResponseFormats.Select(format => format.MediaType).Order(StringComparer.Ordinal));
    }

    private static ApiDescription FindDescription(ApiDescription[] descriptions, string endpointName)
    {
        return Assert.Single(descriptions, description => description.ActionDescriptor.EndpointMetadata.OfType<IEndpointNameMetadata>().Any(metadata => metadata.EndpointName == endpointName));
    }

    private sealed class ForeignEndpoint : IGetEndpoint
    {
        [StringSyntax("Route")]
        public string Pattern => "/foreign";

        public string HandleAsync() => "foreign";
    }
}