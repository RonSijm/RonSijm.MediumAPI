using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using static RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.EndpointDocumentationHost;
using ExampleProgram = RonSijm.MediumAPI.EndpointDocumentationExample.Program;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest;

public sealed class StartupConventionPolicyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Six_host_selected_profiles_match_native_ordered_types_media_API_Explorer_and_complete_documents(bool json)
    {
        await using var native = CreateSample(json, native: true);
        await using var generated = CreateSample(json);
        await native.StartAsync();
        await generated.StartAsync();
        Assert.Equal(MetadataInventory(native), MetadataInventory(generated));
        foreach (var path in SamplePaths())
        {
            var expected = Route(native, path);
            var actual = Route(generated, path);
            Assert.Equal(Responses(expected), Responses(actual));
            Assert.Equal(expected.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, actual.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
            Assert.Equal(expected.Metadata.GetMetadata<MethodInfo>()!.ReturnType, actual.Metadata.GetMetadata<MethodInfo>()!.ReturnType);
            if (json) { Assert.NotEmpty(Responses(actual)); }
        }

        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        foreach (var document in new[] { DocumentationGroups.Public, DocumentationGroups.Admin }) { Equivalent(MediumApiContractComparison.CompareOpenApi(ExampleProgram.GetOpenApiJson(native.Services, document), ExampleProgram.GetOpenApiJson(generated.Services, document))); }

        Assert.Equal(0, generated.Services.GetRequiredService<StartupConventionProbe>().Constructions);
        Assert.Equal(0, generated.Services.GetRequiredService<StartupConventionProbe>().Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Typed_and_dynamic_handlers_keep_the_same_status_headers_and_bytes_without_request_time_conventions(bool json)
    {
        await using var native = CreateSample(json, native: true);
        await using var generated = CreateSample(json);
        await native.StartAsync();
        await generated.StartAsync();
        using var nativeClient = native.GetTestClient();
        using var client = generated.GetTestClient();
        foreach (var path in SamplePaths())
        {
            using var expected = await Send(nativeClient, path);
            using var actual = await Send(client, path);
            Assert.Equal(expected.StatusCode, actual.StatusCode);
            Assert.Equal(expected.Headers.Location, actual.Headers.Location);
            Assert.Equal(expected.Content.Headers.ContentType?.ToString(), actual.Content.Headers.ContentType?.ToString());
            Assert.Equal(await expected.Content.ReadAsByteArrayAsync(), await actual.Content.ReadAsByteArrayAsync());
        }

        Assert.Equal(1, generated.Services.GetRequiredService<StartupConventionProbe>().Constructions);
        Assert.Equal(7, generated.Services.GetRequiredService<StartupConventionProbe>().Calls);
    }

    [Fact]
    public async Task Host_presentation_identity_is_shared_with_its_alias_and_workspace_but_not_another_host()
    {
        await using var first = CreateSample(true);
        await using var second = CreateSample(false);
        await first.StartAsync();
        await second.StartAsync();
        var presentation = Route(first, "/conventions/token").Metadata.GetMetadata<SamplePresentation>();
        Assert.True(presentation!.UsesJsonResponses);
        foreach (var path in SamplePaths())
        {
            var metadata = Route(first, path).Metadata;
            Assert.Same(presentation, metadata.GetMetadata<SamplePresentation>());
            Assert.Equal(path.EndsWith("/workspace", StringComparison.Ordinal) ? Array.Empty<string>() : new[] { "group" }, metadata.GetOrderedMetadata<ConventionMarker>().Select(value => value.Value));
        }

        Assert.NotSame(presentation, Route(second, "/conventions/token").Metadata.GetMetadata<SamplePresentation>());
        Assert.False(Route(second, "/conventions/token").Metadata.GetMetadata<SamplePresentation>()!.UsesJsonResponses);
        Assert.Null(Route(first, "/conventions/authenticate-alias").Metadata.GetMetadata<IEndpointNameMetadata>());
        var original = Route(first, "/conventions/authenticate").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        var alias = Route(first, "/conventions/authenticate-alias").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        Assert.Equal(3, original.Count);
        for (var index = 0; index < original.Count; index++) { Assert.NotSame(original[index], alias[index]); }
    }

    [Fact]
    public async Task Workspace_fluent_registration_reads_metadata_added_before_it_and_retains_void_status_profiles()
    {
        await using var app = CreateSample(true);
        await app.StartAsync();
        var declarations = Route(app, "/conventions/workspace").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();
        Assert.Equal(new[] { 204, 400, 401, 404 }, declarations.TakeLast(4).Select(value => value.StatusCode));
        Assert.All(declarations.TakeLast(4), value =>
        {
            Assert.Equal(typeof(void), value.Type);
            Assert.Empty(value.ContentTypes);
        });
    }

    [Fact]
    public async Task Disabled_sample_does_not_map_its_namespace_or_manual_aliases_or_run_throwing_conventions()
    {
        await using var app = CreateSample(true, enabled: false);
        Assert.Null(app.TryMapMediumApiNamespace<StartupFixtures.Disabled.DisabledConventionGroupDefinition>());
        await app.StartAsync();
        Assert.Empty(Routes(app));
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/conventions/token");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, app.Services.GetRequiredService<StartupConventionProbe>().Calls);
    }

    [Fact]
    public async Task Default_example_keeps_its_original_routes_when_the_new_host_mode_is_not_selected()
    {
        await using var app = Create();
        await app.StartAsync();
        Assert.Equal(8, Routes(app).Count());
        Assert.DoesNotContain(Routes(app), route => route.RoutePattern.RawText!.StartsWith("/conventions", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Typed_fluent_builders_keep_identity_and_null_builders_fail_before_Configure()
    {
        await using var app = CreateSample(true, enabled: false);
        var group = app.MapGroup("/fluent");
        Assert.Same(group, group.AddMediumApiConvention<StartupFixtures.ExplicitConvention>());
        var endpoint = group.MapGet("/value", () => TypedResults.Ok("fixed"));
        Assert.Same(endpoint, endpoint.AddMediumApiConvention<StartupFixtures.StructConvention>());
        Assert.Throws<ArgumentNullException>(() => ((RouteHandlerBuilder)null!).AddMediumApiConvention<StartupFixtures.ThrowingConvention>());
        Assert.Throws<ArgumentNullException>(() => ((RouteGroupBuilder)null!).AddMediumApiConvention<StartupFixtures.ThrowingConvention>());
        await app.StartAsync();
        Assert.Equal(new[] { "explicit", "struct" }, Route(app, "/fluent/value").Metadata.GetOrderedMetadata<ConventionMarker>().Select(value => value.Value));
    }

    [Fact]
    public async Task Repeated_declarations_configure_once_per_mapping_while_native_callbacks_can_materialize_again()
    {
        StartupFixtures.RepeatedConvention.Reset();
        await using var app = CreateSample(true, enabled: false);
        app.MapMediumApiEndpoint<StartupFixtures.RepeatedConventionEndpoint>("/repeat/one").WithoutDefaultEndpointName();
        app.MapMediumApiEndpoint<StartupFixtures.RepeatedConventionEndpoint>("/repeat/two").WithoutDefaultEndpointName();
        Assert.Equal(4, StartupFixtures.RepeatedConvention.Configurations);
        foreach (var route in Routes(app)) { Assert.Equal(2, route.Metadata.GetOrderedMetadata<ConventionMarker>().Count); }

        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/repeat/one");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(4, StartupFixtures.RepeatedConvention.Configurations);
    }

    [Fact]
    public async Task Original_method_inheritance_remains_while_class_and_namespace_base_declarations_are_ignored()
    {
        await using var app = CreateSample(true, enabled: false);
        var group = app.MapMediumApiNamespace<StartupFixtures.Inheritance.InheritedConventionGroupDefinition>();
        group.MapMediumApiEndpoint<StartupFixtures.InheritedConventionEndpoint>();
        await app.StartAsync();
        Assert.Equal(new[] { "method" }, Route(app, "/inherited-conventions/value").Metadata.GetOrderedMetadata<ConventionMarker>().Select(value => value.Value));
    }

    [Fact]
    public async Task Convention_type_attributes_are_not_implicitly_imported_and_Configure_failures_keep_original_identity()
    {
        await using var app = CreateSample(true, enabled: false);
        app.MapGet("/metadata", () => TypedResults.Ok("fixed")).AddMediumApiConvention<StartupFixtures.NoImplicitMetadataConvention>();
        var endpoint = app.MapGet("/failure", () => TypedResults.Ok("fixed"));
        Assert.Same(StartupFixtures.ThrowingConvention.Failure, Assert.Throws<InvalidOperationException>(() => endpoint.AddMediumApiConvention<StartupFixtures.ThrowingConvention>()));
        Assert.Same(StartupFixtures.ThrowingConvention.Failure, Assert.Throws<InvalidOperationException>(() => app.MapMediumApiEndpoint<StartupFixtures.ThrowingConventionEndpoint>()));
        await app.StartAsync();
        var metadata = Route(app, "/metadata").Metadata;
        Assert.Equal(new[] { "explicit-only" }, metadata.GetOrderedMetadata<ConventionMarker>().Select(value => value.Value));
        Assert.DoesNotContain(metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), value => value.StatusCode == 418);
        Assert.Null(metadata.GetMetadata<ITagsMetadata>());
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/metadata");
        Assert.False(response.Headers.Contains("X-Unused-Convention"));
    }

    [Fact]
    public async Task Host_Add_and_Finally_keep_native_precedence_after_the_declared_convention()
    {
        await using var app = CreateSample(true, enabled: false);
        var group = app.MapGroup("/precedence");
        ((IEndpointConventionBuilder)group).Add(builder => builder.Metadata.Add(new ConventionMarker("host-group-add")));
        ((IEndpointConventionBuilder)group).Finally(builder => builder.Metadata.Add(new ConventionMarker("host-group-final")));
        var endpoint = group.MapGet("/value", () => TypedResults.Ok("fixed")).AddMediumApiConvention<StartupFixtures.StructConvention>();
        endpoint.Add(builder => builder.Metadata.Add(new ConventionMarker("host-endpoint-add")));
        endpoint.Finally(builder => builder.Metadata.Add(new ConventionMarker("host-endpoint-final")));
        await app.StartAsync();
        Assert.Equal(new[] { "host-group-add", "struct", "host-endpoint-add", "host-endpoint-final", "host-group-final" }, Route(app, "/precedence/value").Metadata.GetOrderedMetadata<ConventionMarker>().Select(value => value.Value));
    }

    [Fact]
    public async Task Mixed_group_class_method_and_filter_declarations_keep_native_registration_order()
    {
        await using var app = CreateSample(true, enabled: false);
        await using var native = CreateSample(true, enabled: false);
        var group = app.MapGroup("/mixed").AddMediumApiConvention<DocumentScopeConvention>().AddMediumApiFilter<StartupFixtures.TraceFilter>().AddMediumApiConvention<StartupFixtures.ExplicitConvention>();
        group.MapMediumApiEndpoint<StartupFixtures.OrderedConventionEndpoint>();
        var nativeGroup = native.MapGroup("/mixed");
        ((IEndpointConventionBuilder)nativeGroup).Add(builder => builder.Metadata.Add(new ConventionMarker("group")));
        ((IEndpointConventionBuilder)nativeGroup).Add(builder => builder.Metadata.Add(new ConventionMarker("filter")));
        nativeGroup.AddEndpointFilter<StartupFixtures.TraceFilter>();
        ((IEndpointConventionBuilder)nativeGroup).Add(builder => builder.Metadata.Add(new ConventionMarker("explicit")));
        var endpoint = nativeGroup.MapGet("/value", NativeFixed).WithName("OrderedConvention");
        endpoint.Add(builder => builder.Metadata.Add(new ConventionMarker("class")));
        endpoint.Add(builder => builder.Metadata.Add(new ConventionMarker("filter")));
        endpoint.AddEndpointFilter<StartupFixtures.TraceFilter>();
        endpoint.Add(builder => builder.Metadata.Add(new ConventionMarker("struct")));
        endpoint.Add(builder => builder.Metadata.Add(new ConventionMarker("filter")));
        endpoint.AddEndpointFilter<StartupFixtures.TraceFilter>();
        endpoint.Add(builder => builder.Metadata.Add(new ConventionMarker("method")));
        await app.StartAsync();
        await native.StartAsync();
        var expected = Route(native, "/mixed/value").Metadata.GetOrderedMetadata<ConventionMarker>().Select(value => value.Value).ToArray();
        Assert.Equal(new[] { "group", "filter", "explicit", "class", "filter", "struct", "filter", "method" }, expected);
        Assert.Equal(expected, Route(app, "/mixed/value").Metadata.GetOrderedMetadata<ConventionMarker>().Select(value => value.Value));
        Assert.Equal(Responses(Route(native, "/mixed/value")), Responses(Route(app, "/mixed/value")));
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/mixed/value");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static WebApplication CreateSample(bool json, bool native = false, bool enabled = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Sample:StartupConventions"] = enabled.ToString(), ["Sample:JsonResponses"] = json.ToString() });
        }, mapEndpoints: false);
        if (native) { MapNativeSample(app, json, enabled); }
        else { StartupConventionSample.Map(app); }

        return app;
    }

    private static string[] Responses(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(value => value.StatusCode + "|" + value.Type?.AssemblyQualifiedName + "|" + string.Join(",", value.ContentTypes)).ToArray();
    }

    private static string[] SamplePaths()
    {
        return new[] { "/conventions/token", "/conventions/authenticate", "/conventions/request-options", "/conventions/challenge", "/conventions/logout", "/conventions/authenticate-alias", "/conventions/workspace" };
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string path)
    {
        return path.EndsWith("/token", StringComparison.Ordinal) || path.EndsWith("/challenge", StringComparison.Ordinal) ? client.GetAsync(path) : client.PostAsync(path, content: null);
    }

    private static void MapNativeSample(WebApplication app, bool json, bool enabled)
    {
        if (!enabled) { return; }

        var presentation = new SamplePresentation(json);
        var group = app.MapGroup("/conventions").WithGroupName(DocumentationGroups.Public).WithTags("Startup conventions").WithMetadata(presentation);
        ((IEndpointConventionBuilder)group).Add(builder => builder.Metadata.Add(new ConventionMarker("group")));
        var token = group.MapGet("/token", NativeToken).WithName("ConventionToken");
        var authentication = group.MapPost("/authenticate", NativeAuthentication).WithName("ConventionAuthentication");
        var requestOptions = group.MapPost("/request-options", NativeRequestOptions).WithName("ConventionRequestOptions");
        var challenge = group.MapGet("/challenge", NativeChallenge).WithName("ConventionChallenge");
        var logout = group.MapPost("/logout", NativeLogout).WithName("ConventionLogout");
        var alias = group.MapPost("/authenticate-alias", NativeAuthentication);
        var workspace = app.MapPost("/conventions/workspace", NativeWorkspace).WithName("ConventionWorkspace").WithGroupName(DocumentationGroups.Public).WithTags("Startup conventions").WithMetadata(presentation);
        if (!json) { return; }

        token.Produces<SampleTokenResponse>(200, "application/json");
        foreach (var endpoint in new[] { authentication, alias }) { endpoint.Produces<SampleAuthenticationResponse>(200, "application/json").ProducesProblem(400).ProducesProblem(401); }

        requestOptions.Produces(200, contentType: "application/json").ProducesProblem(400);
        challenge.Produces(302).ProducesProblem(400);
        logout.Produces(204).ProducesProblem(400);
        workspace.Produces(204).Produces(400).Produces(401).Produces(404);
    }

    private static JsonHttpResult<SampleTokenResponse> NativeToken(StartupConventionProbe probe)
    {
        probe.Calls++;
        return TypedResults.Json(new SampleTokenResponse("Sample-Header", "fixed-synthetic-token"));
    }

    private static IResult NativeAuthentication(HttpContext context, StartupConventionProbe probe)
    {
        probe.Calls++;
        return context.GetEndpoint()?.Metadata.GetMetadata<SamplePresentation>()?.UsesJsonResponses == true ? Results.Ok(new SampleAuthenticationResponse("ready", null)) : Results.Redirect("/sign-in");
    }

    private static IResult NativeRequestOptions(StartupConventionProbe probe)
    {
        probe.Calls++;
        return Results.Json(new { challenge = "fixed-synthetic-challenge" });
    }

    private static RedirectHttpResult NativeChallenge(StartupConventionProbe probe)
    {
        probe.Calls++;
        return TypedResults.Redirect("/external-sign-in");
    }

    private static Results<BadRequest, ProblemHttpResult, RedirectHttpResult, NoContent> NativeLogout(HttpContext context, StartupConventionProbe probe)
    {
        probe.Calls++;
        return context.GetEndpoint()?.Metadata.GetMetadata<SamplePresentation>()?.UsesJsonResponses == true ? TypedResults.NoContent() : TypedResults.Redirect("/signed-out");
    }

    private static Results<BadRequest, NotFound, RedirectHttpResult, NoContent> NativeWorkspace(HttpContext context, StartupConventionProbe probe)
    {
        probe.Calls++;
        return context.GetEndpoint()?.Metadata.GetMetadata<SamplePresentation>()?.UsesJsonResponses == true ? TypedResults.NoContent() : TypedResults.Redirect("/workspace");
    }

    private static Ok<string> NativeFixed()
    {
        return TypedResults.Ok("fixed");
    }
}