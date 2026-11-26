using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest.FactoryAliasFixtures;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.ResourcePoliciesExample.Program;
using NamedFixtures = RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest.FactoryAliasFixtures.Named;

namespace RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest;

public sealed class FactoryAliasTests
{
    [Fact]
    public void The_actual_generic_marker_is_derivable_and_document_aliases_keep_the_same_fixed_static_targets()
    {
        Assert.False(typeof(EndpointFilterFactoryAttribute<>).IsSealed);
        Assert.Equal(typeof(EndpointFilterAttribute), typeof(EndpointFilterFactoryAttribute<>).BaseType);
        foreach (var pair in new[] { (typeof(DocumentProjectGuardAttribute), typeof(DocumentProjectGuardFactory)), (typeof(DocumentCustomerGuardAttribute), typeof(DocumentCustomerGuardFactory)) })
        {
            Assert.Equal(typeof(EndpointFilterFactoryAttribute<>).MakeGenericType(pair.Item2), pair.Item1.BaseType);
            Assert.Empty(pair.Item1.GetCustomAttributes<AttributeUsageAttribute>(inherit: false));
            var usage = pair.Item1.GetCustomAttribute<AttributeUsageAttribute>(inherit: true)!;
            Assert.Equal(AttributeTargets.Class | AttributeTargets.Method, usage.ValidOn);
            Assert.True(usage.AllowMultiple);
            Assert.True(usage.Inherited);
            Assert.Equal(Type.EmptyTypes, pair.Item1.GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType));
        }

        var assembly = typeof(DocumentProjectGuardAttribute).Assembly;
        Assert.IsType<DocumentProjectGuardAttribute>(Assert.Single(assembly.GetType("RonSijm.MediumAPI.ResourcePoliciesExample.DocumentPages.ProjectDocumentEndpoint")!.GetMethod("HandleAsync")!.GetCustomAttributes<EndpointFilterAttribute>(inherit: false)));
        Assert.IsType<DocumentCustomerGuardAttribute>(Assert.Single(assembly.GetType("RonSijm.MediumAPI.ResourcePoliciesExample.DocumentPages.CustomerDocumentEndpoint")!.GetMethod("HandleAsync")!.GetCustomAttributes<EndpointFilterAttribute>(inherit: false)));
    }

    [Fact]
    public async Task Named_generic_and_native_original_delegate_hosts_keep_complete_route_and_API_Explorer_contracts()
    {
        await using var native = FactoryAliasHost.Create("native");
        await native.StartAsync();
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = FactoryAliasHost.Create(reference);
            await app.StartAsync();
            Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
            Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(app)));
            Assert.Equal(Responses(Route(native)), Responses(Route(app)));
            Assert.Equal(typeof(Microsoft.AspNetCore.Http.HttpResults.Ok<FactoryAliasReceipt>), Route(app).Metadata.GetMetadata<MethodInfo>()!.ReturnType);
            Assert.Equal(new[] { "customerId", "take", "context", "scope" }, Route(app).Metadata.GetMetadata<MethodInfo>()!.GetParameters().Select(parameter => parameter.Name));
        }
    }

    [Theory]
    [InlineData(7L, HttpStatusCode.OK)]
    [InlineData(0L, HttpStatusCode.NotFound)]
    [InlineData(-3L, HttpStatusCode.NotFound)]
    public async Task The_same_guard_result_payload_media_and_existing_header_boundaries_remain(long customerId, HttpStatusCode status)
    {
        await using var native = FactoryAliasHost.Create("native");
        await native.StartAsync();
        using var expectedClient = native.GetTestClient();
        using var expected = await expectedClient.GetAsync("/factory-alias/value/" + customerId + "?take=4");
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = FactoryAliasHost.Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var actual = await client.GetAsync("/factory-alias/value/" + customerId + "?take=4");
            await SameResponse(expected, actual);
            Assert.Equal(status, actual.StatusCode);
            Assert.Equal(status == HttpStatusCode.OK, actual.Headers.Contains("X-Handler"));
            Assert.Equal("2", Assert.Single(actual.Headers.GetValues("X-Factory-Count")));
            if (status == HttpStatusCode.OK) { Assert.Equal(new FactoryAliasReceipt(customerId, 4, 2), await actual.Content.ReadFromJsonAsync<FactoryAliasReceipt>()); }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Factories_read_current_original_arguments_after_an_earlier_native_replacement(bool replace)
    {
        foreach (var reference in new[] { "native", "generic", "named" })
        {
            await using var app = FactoryAliasHost.Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/factory-alias/value/0");
            if (replace) { request.Headers.Add("X-Replace-Customer", "true"); }

            using var response = await client.SendAsync(request);
            Assert.Equal(replace ? HttpStatusCode.OK : HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(replace, response.Headers.Contains("X-Handler"));
            if (replace) { Assert.Equal(7, (await response.Content.ReadFromJsonAsync<FactoryAliasReceipt>())!.CustomerId); }
        }
    }

    [Fact]
    public async Task Native_binding_failures_preserve_their_status_and_do_not_enter_the_original_handler()
    {
        await using var native = FactoryAliasHost.Create("native");
        await native.StartAsync();
        using var expectedClient = native.GetTestClient();
        using var expected = await expectedClient.GetAsync("/factory-alias/value/7?take=invalid");
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = FactoryAliasHost.Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/factory-alias/value/7?take=invalid");
            await SameResponse(expected, response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(response.Headers.Contains("X-Handler"));
        }
    }

    [Theory]
    [InlineData("not-long")]
    [InlineData("9223372036854775808")]
    public async Task Native_route_constraints_fail_before_factory_or_handler_policies(string customerId)
    {
        foreach (var reference in new[] { "native", "generic", "named" })
        {
            await using var app = FactoryAliasHost.Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/factory-alias/value/" + customerId);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.False(response.Headers.Contains("X-Factory-Declaration"));
            Assert.False(response.Headers.Contains("X-Handler"));
        }
    }

    [Fact]
    public async Task Existing_combined_factory_metadata_and_Configure_flow_but_alias_type_decorations_are_not_imported()
    {
        await using var app = FactoryAliasHost.Create();
        await app.StartAsync();
        var route = Route(app);
        Assert.Equal(new[] { "configure", "declared" }, route.Metadata.GetOrderedMetadata<FactoryStageAttribute>().Select(value => value.Value));
        Assert.Contains(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), value => value.StatusCode == 418);
        Assert.Contains(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), value => value.StatusCode == 419);
        Assert.DoesNotContain(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), value => value.StatusCode == 499);
        Assert.Single(route.Metadata.GetOrderedMetadata<Microsoft.AspNetCore.Mvc.ConsumesAttribute>());
        Assert.False(route.Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        Assert.Equal(2, typeof(NamedFixtures.ValueEndpoint).GetCustomAttributes<EndpointFilterAttribute>(inherit: false).Count() + typeof(NamedFixtures.GroupDefinition).GetCustomAttributes<EndpointFilterAttribute>(inherit: false).Count());
        Assert.Contains(typeof(NamedFixtures.ValueEndpoint).GetMethod("HandleAsync")!.GetCustomAttributes<EndpointFilterAttribute>(), value => value is DocumentCustomerGuardAttribute);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/factory-alias/value/7");
        Assert.False(response.Headers.Contains("X-Ignored-Alias"));
        Assert.Equal("present", Assert.Single(response.Headers.GetValues("X-Factory-Declaration")));
        Assert.Equal("HandleAsync", Assert.Single(response.Headers.GetValues("X-Factory-Created-For")));
    }

    [Fact]
    public async Task Repeated_static_struct_factories_keep_current_invocation_state_and_each_request_has_native_scoped_services()
    {
        foreach (var reference in new[] { "native", "generic", "named" })
        {
            await using var app = FactoryAliasHost.Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var first = await client.GetAsync("/factory-alias/value/7");
            using var second = await client.GetAsync("/factory-alias/value/7");
            Assert.Equal("2", Assert.Single(first.Headers.GetValues("X-Factory-Count")));
            Assert.Equal("2", Assert.Single(second.Headers.GetValues("X-Factory-Count")));
            Assert.NotEqual(Assert.Single(first.Headers.GetValues("X-Request-Scope")), Assert.Single(second.Headers.GetValues("X-Request-Scope")));
            Assert.Equal(await first.Content.ReadAsByteArrayAsync(), await second.Content.ReadAsByteArrayAsync());
        }
    }

    [Fact]
    public async Task Explicit_aliases_repeat_factory_registration_without_reusing_names_or_cross_host_state()
    {
        foreach (var reference in new[] { "native", "generic", "named" })
        {
            await using var first = FactoryAliasHost.Create(reference, alias: true);
            await using var second = FactoryAliasHost.Create(reference, alias: true);
            await first.StartAsync();
            await second.StartAsync();
            Assert.Equal("FactoryAliasValue", Route(first).Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
            Assert.Null(Route(first, "/factory-alias/alias/{customerId:long}").Metadata.GetMetadata<IEndpointNameMetadata>());
            Assert.Equal(Responses(Route(first)), Responses(Route(first, "/factory-alias/alias/{customerId:long}")));
            Assert.NotSame(Route(first).Metadata.GetMetadata<FactoryStageAttribute>(), Route(second).Metadata.GetMetadata<FactoryStageAttribute>());
            using var client = first.GetTestClient();
            using var response = await client.GetAsync("/factory-alias/alias/7");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("2", Assert.Single(response.Headers.GetValues("X-Factory-Count")));
        }
    }

    [Fact]
    public async Task Method_override_factory_counts_follow_the_exact_original_native_declaration_reader()
    {
        var old = typeof(GenericOverrideEndpoint).GetMethod("HandleAsync")!.GetCustomAttributes(inherit: true).OfType<EndpointFilterAttribute>().ToArray();
        var named = typeof(NamedOverrideEndpoint).GetMethod("HandleAsync")!.GetCustomAttributes(inherit: true).OfType<EndpointFilterAttribute>().ToArray();
        Assert.Equal(old.Select(Target), named.Select(Target));
        Assert.All(old, value => Assert.Equal(typeof(EndpointFilterFactoryAttribute<RepeatedFactory>), value.GetType()));
        foreach (var reference in new[] { "native", "generic", "named" })
        {
            await using var app = FactoryAliasHost.Create(enabled: false);
            if (reference == "native")
            {
                var endpoint = app.MapGet("/override", (Delegate)(Func<HttpContext, Microsoft.AspNetCore.Http.HttpResults.Ok<int>>)CountBody);
                foreach (var declaration in old) { endpoint.AddEndpointFilterFactory(RepeatedFactory.Create); }
            }
            else if (reference == "generic") { app.MapMediumApiEndpoint<GenericOverrideEndpoint>(); }
            else { app.MapMediumApiEndpoint<NamedOverrideEndpoint>(); }

            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/override");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(old.Length, await response.Content.ReadFromJsonAsync<int>());
            Assert.Equal(old.Length.ToString(), Assert.Single(response.Headers.GetValues("X-Factory-Count")));
        }
    }

    [Fact]
    public async Task Static_Configure_failure_keeps_the_original_exception_object_without_constructing_the_factory()
    {
        await using var app = FactoryAliasHost.Create(enabled: false);
        Assert.Same(FailingConfigureFactory.Failure, Assert.Throws<InvalidOperationException>(() => app.MapMediumApiEndpoint<FailingConfigureEndpoint>()));
        var native = app.MapGet("/native-configure", () => TypedResults.Ok(1));
        Assert.Same(FailingConfigureFactory.Failure, Assert.Throws<InvalidOperationException>(() => FailingConfigureFactory.Configure(native)));
    }

    [Fact]
    public async Task Static_Create_failure_retains_native_exception_identity_at_pipeline_materialization()
    {
        foreach (var named in new[] { false, true })
        {
            await using var app = FactoryAliasHost.Create(enabled: false);
            if (named) { app.MapMediumApiEndpoint<FailingCreateEndpoint>(); }
            else { app.MapGet("/create-failure", () => TypedResults.Ok(1)).AddEndpointFilterFactory(FailingCreateFactory.Create); }

            Assert.Same(FailingCreateFactory.Failure, Assert.Throws<InvalidOperationException>(() => Routes(app).ToArray()));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task The_existing_public_document_sample_uses_aliases_and_preserves_availability_and_entry_headers(bool enabled)
    {
        foreach (var legacy in new[] { true, false })
        {
            await using var app = ExampleProgram.Build(new[] { "--Sample:DocumentsEnabled=" + enabled }, configure: builder =>
            {
                builder.WebHost.UseTestServer();
                builder.Logging.ClearProviders();
            }, legacy: legacy);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/sample/documents/projects/7/9");
            Assert.Equal(enabled ? HttpStatusCode.OK : HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(enabled, response.Headers.CacheControl?.NoStore == true);
            Assert.Equal(enabled ? 1 : 0, app.Services.GetRequiredService<DocumentPageProbe>().Entries);
            if (enabled) { Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType); }
        }
    }

    private static IEnumerable<RouteEndpoint> Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    }

    private static RouteEndpoint Route(WebApplication app, string path = "/factory-alias/value/{customerId:long}")
    {
        return Assert.Single(Routes(app), value => value.RoutePattern.RawText == path);
    }

    private static MediumApiDescriptionSnapshot Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    }

    private static string[] Responses(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(value => value.StatusCode + "|" + value.Type?.AssemblyQualifiedName + "|" + string.Join(",", value.ContentTypes)).ToArray();
    }

    private static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(value => value.Path + " " + value.Kind)));
    }

    private static Type Target(EndpointFilterAttribute attribute)
    {
        for (var type = attribute.GetType(); type is not null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EndpointFilterFactoryAttribute<>)) { return Assert.Single(type.GenericTypeArguments); }
        }

        throw new InvalidOperationException("The fixture requires a factory declaration.");
    }

    private static Microsoft.AspNetCore.Http.HttpResults.Ok<int> CountBody(HttpContext context)
    {
        return TypedResults.Ok(context.Items.TryGetValue("factory-count", out var current) ? (int)current! : 0);
    }

    private static async Task SameResponse(HttpResponseMessage expected, HttpResponseMessage actual)
    {
        Assert.Equal(expected.StatusCode, actual.StatusCode);
        Assert.Equal(expected.Content.Headers.ContentType?.ToString(), actual.Content.Headers.ContentType?.ToString());
        Assert.Equal(await expected.Content.ReadAsByteArrayAsync(), await actual.Content.ReadAsByteArrayAsync());
        foreach (var name in new[] { "X-Factory-Declaration", "X-Factory-Created-For", "X-Factory-Count", "X-Handler" })
        {
            Assert.Equal(expected.Headers.TryGetValues(name, out var left) ? left.ToArray() : Array.Empty<string>(), actual.Headers.TryGetValues(name, out var right) ? right.ToArray() : Array.Empty<string>());
        }
    }
}