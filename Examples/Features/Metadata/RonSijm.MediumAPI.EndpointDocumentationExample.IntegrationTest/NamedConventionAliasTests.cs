using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using Xunit.Abstractions;
using static RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.NamedConventionHost;
using ExampleProgram = RonSijm.MediumAPI.EndpointDocumentationExample.Program;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest;

public sealed class NamedConventionAliasTests
{
    private readonly ITestOutputHelper output;

    public NamedConventionAliasTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public async Task The_actual_generic_marker_is_derivable_and_named_declarations_retain_native_usage_on_all_three_owners()
    {
        var generic = typeof(EndpointConventionAttribute<>);
        Assert.False(generic.IsSealed);
        Assert.Equal(typeof(EndpointConventionAttribute), generic.BaseType);
        var native = generic.GetCustomAttribute<AttributeUsageAttribute>(inherit: true)!;
        Assert.Equal(AttributeTargets.Class | AttributeTargets.Method, native.ValidOn);
        Assert.True(native.AllowMultiple);
        Assert.True(native.Inherited);
        foreach (var name in new[] { "NamedScopeAttribute", "NamedClassAttribute", "NamedMethodAttribute", "NamedStructAttribute", "NamedExplicitClassAttribute" })
        {
            var alias = AliasType(name);
            Assert.Equal(generic, alias.BaseType!.GetGenericTypeDefinition());
            var usage = alias.GetCustomAttribute<AttributeUsageAttribute>(inherit: true)!;
            Assert.Equal(native.ValidOn, usage.ValidOn);
            Assert.Equal(native.AllowMultiple, usage.AllowMultiple);
            Assert.Equal(native.Inherited, usage.Inherited);
        }

        await using var app = Create();
        await app.StartAsync();
        var method = Route(app, "/named-conventions/value").Metadata.GetMetadata<MethodInfo>()!;
        Assert.Contains(method.GetCustomAttributes<EndpointConventionAttribute>(inherit: false), value => value.GetType() == AliasType("NamedMethodAttribute"));
        Assert.Contains(method.DeclaringType!.GetCustomAttributes<EndpointConventionAttribute>(inherit: false), value => value.GetType() == AliasType("NamedClassAttribute"));
        var group = Assert.Single(typeof(AliasConventionStage).Assembly.GetTypes(), type => typeof(INamespaceEndpointGroup).IsAssignableFrom(type) && type.GetCustomAttributesData().Any(value => value.AttributeType == AliasType("NamedScopeAttribute")));
        Assert.IsAssignableFrom<EndpointConventionAttribute>(Assert.Single(group.GetCustomAttributes<EndpointConventionAttribute>(inherit: false)));
    }

    [Fact]
    public async Task Named_generic_and_independent_native_mappings_keep_complete_routes_and_API_Explorer_contracts()
    {
        await using var native = Create("native");
        await native.StartAsync();
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
            Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(app)));
            Assert.Equal(4, Routes(app).Count());
            foreach (var path in Paths())
            {
                Assert.Equal(Stages(Route(native, path)), Stages(Route(app, path)));
                Assert.Equal(Route(native, path).Metadata.GetMetadata<MethodInfo>()!.ReturnType, Route(app, path).Metadata.GetMetadata<MethodInfo>()!.ReturnType);
            }

            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Theory]
    [InlineData(DocumentationGroups.Public)]
    [InlineData(DocumentationGroups.Admin)]
    public async Task Entire_Swagger_documents_match_without_hiding_metadata_or_schema_differences(string document)
    {
        await using var native = Create("native");
        await native.StartAsync();
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            Equivalent(MediumApiContractComparison.CompareOpenApi(OpenApi(native, document), OpenApi(app, document)));
        }
    }

    [Theory]
    [InlineData("value")]
    [InlineData("repeated")]
    [InlineData("inherited")]
    [InlineData("overridden")]
    public async Task Named_aliases_keep_the_same_HTTP_headers_payloads_and_guard_handler_order_as_existing_registrations(string kind)
    {
        await using var native = Create("native");
        await native.StartAsync();
        using var nativeClient = native.GetTestClient();
        using var expected = await nativeClient.GetAsync("/named-conventions/" + kind + "?page=2");
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var actual = await client.GetAsync("/named-conventions/" + kind + "?page=2");
            await SameResponse(expected, actual);
            Assert.Equal(HttpStatusCode.OK, actual.StatusCode);
            var reply = (await actual.Content.ReadFromJsonAsync<AliasConventionReply>())!;
            Assert.Equal(kind, reply.Kind);
            Assert.Equal(2, reply.Page);
            Assert.Equal(Stages(Route(app, "/named-conventions/" + kind)), reply.Stages);
            Assert.Equal("handler", Assert.Single(actual.Headers.GetValues("X-Alias-Handler")));
            Assert.Equal(Probe(native).Events, Probe(app).Events);
            Assert.Equal(new[] { "guard-enter", "handler:" + kind, "guard-exit" }, Probe(app).Events);
            Assert.Equal(1, Probe(app).Calls);
        }
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("?page=0", 0)]
    [InlineData("?page=12", 12)]
    public async Task Native_optional_binding_is_unchanged_by_the_named_marker(string query, int? page)
    {
        foreach (var reference in new[] { "native", "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/named-conventions/value" + query);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(page, (await response.Content.ReadFromJsonAsync<AliasConventionReply>())!.Page);
            Assert.Equal(1, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Earlier_native_guard_short_circuits_before_later_header_policies_and_handler_entry()
    {
        await using var native = Create("native");
        await native.StartAsync();
        using var nativeClient = native.GetTestClient();
        using var expected = await Reject(nativeClient);
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var actual = await Reject(client);
            await SameResponse(expected, actual);
            Assert.Equal(HttpStatusCode.Forbidden, actual.StatusCode);
            Assert.Empty(Headers(actual));
            Assert.Equal(new[] { "guard-enter", "guard-reject" }, Probe(app).Events);
            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Binding_failure_keeps_native_outer_headers_and_does_not_enter_the_handler()
    {
        await using var native = Create("native");
        await native.StartAsync();
        using var nativeClient = native.GetTestClient();
        using var expected = await nativeClient.GetAsync("/named-conventions/value?page=invalid");
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var actual = await client.GetAsync("/named-conventions/value?page=invalid");
            await SameResponse(expected, actual);
            Assert.Equal(HttpStatusCode.BadRequest, actual.StatusCode);
            Assert.False(actual.Headers.Contains("X-Alias-Handler"));
            Assert.Equal(Probe(native).Events, Probe(app).Events);
            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Repeated_named_markers_and_explicit_class_and_struct_conventions_all_dispatch_without_activation()
    {
        foreach (var reference in new[] { "native", "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            var stages = Stages(Route(app, "/named-conventions/repeated"));
            Assert.Equal(2, stages.Count(value => value == "method"));
            Assert.Contains("explicit-struct", stages);
            Assert.Contains("explicit-class", stages);
            Assert.DoesNotContain("class", stages);
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/named-conventions/repeated");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Native_method_usage_controls_unique_repeated_and_noninherited_aliases_while_class_base_policies_stay_direct_only()
    {
        var method = typeof(NamedUsageDerivedEndpoint).GetMethod("HandleAsync")!;
        var declarations = method.GetCustomAttributes<EndpointConventionAttribute>(inherit: true).ToArray();
        Assert.Equal("derived", Assert.Single(declarations.OfType<UniqueNamedAttribute>()).Owner);
        Assert.Empty(declarations.OfType<LocalNamedAttribute>());
        Assert.Equal(2, declarations.OfType<RepeatedNamedAttribute>().Count());
        await using var app = Create(map: false);
        app.MapMediumApiEndpoint<NamedUsageDerivedEndpoint>();
        await app.StartAsync();
        var stages = Stages(Route(app, "/named-usage"));
        Assert.Equal(1, stages.Count(value => value == "usage-unique"));
        Assert.Equal(2, stages.Count(value => value == "usage-repeat"));
        Assert.DoesNotContain("usage-local", stages);
        Assert.DoesNotContain("usage-class-base", stages);
    }

    [Fact]
    public async Task Ordinary_Add_and_Finally_callbacks_keep_their_native_owner_order_and_materialize_without_requests()
    {
        await using var native = Create("native");
        await native.StartAsync();
        foreach (var reference in new[] { "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            var stages = Stages(Route(app, "/named-conventions/value"));
            Assert.Equal(Stages(Route(native, "/named-conventions/value")), stages);
            foreach (var stage in new[] { "map", "namespace", "class", "method", "host", "host-finally", "namespace-finally" }) { Assert.Contains(stage, stages); }

            Assert.True(Array.IndexOf(stages, "map") < Array.IndexOf(stages, "namespace"));
            Assert.True(Array.IndexOf(stages, "namespace") < Array.IndexOf(stages, "host"));
            Assert.True(Array.IndexOf(stages, "host") < Array.IndexOf(stages, "class"));
            Assert.True(Array.IndexOf(stages, "class") < Array.IndexOf(stages, "method"));
            Assert.True(Array.IndexOf(stages, "host") < Array.IndexOf(stages, "host-finally"));
            Assert.NotEmpty(Probe(app).Materializations);
            Assert.Empty(Probe(app).Events);
            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Fact]
    public async Task Attributes_on_alias_and_static_convention_types_are_not_implicitly_imported_as_endpoint_policies()
    {
        foreach (var reference in new[] { "native", "generic", "named" })
        {
            await using var app = Create(reference);
            await app.StartAsync();
            foreach (var endpoint in Routes(app))
            {
                Assert.DoesNotContain(endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), metadata => metadata.StatusCode is 418 or 419);
                Assert.DoesNotContain(endpoint.Metadata.GetOrderedMetadata<ITagsMetadata>(), metadata => metadata.Tags.Any(value => value.Contains("Ignored", StringComparison.OrdinalIgnoreCase)));
                Assert.DoesNotContain(Stages(endpoint), value => value.Contains("ignored", StringComparison.OrdinalIgnoreCase));
            }

            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/named-conventions/value");
            Assert.DoesNotContain(response.Headers, pair => pair.Key.Contains("Ignored", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Configure_failure_keeps_the_exact_original_exception_instance_without_wrapping_or_activation()
    {
        foreach (var reference in new[] { "native", "generic", "named" })
        {
            await using var app = Create(map: false);
            Assert.Same(AliasThrowConvention.Failure, Assert.Throws<InvalidOperationException>(() => MapFailure(app, reference)));
            Assert.Equal(0, Probe(app).Calls);
            Assert.Empty(Probe(app).Events);
        }
    }

    [Fact]
    public async Task Aliases_keep_names_repeat_the_same_policies_and_use_fresh_host_state()
    {
        foreach (var reference in new[] { "native", "generic", "named" })
        {
            await using var first = Create(reference, alias: true);
            await using var second = Create(reference);
            await first.StartAsync();
            await second.StartAsync();
            Assert.NotSame(Probe(first), Probe(second));
            Assert.Equal("NamedConventionAlias", Route(first, "/named-conventions/alias").Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
            Assert.Equal(Stages(Route(first, "/named-conventions/value")), Stages(Route(first, "/named-conventions/alias")));
            using var client = first.GetTestClient();
            using var response = await client.GetAsync("/named-conventions/alias?page=4");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(4, (await response.Content.ReadFromJsonAsync<AliasConventionReply>())!.Page);
            Assert.Equal(1, Probe(first).Calls);
            Assert.Equal(0, Probe(second).Calls);
            Assert.DoesNotContain(Routes(second), endpoint => endpoint.RoutePattern.RawText == "/named-conventions/alias");
        }
    }

    [Fact]
    public async Task Disabled_groups_and_public_sample_selection_keep_default_exposure_independent()
    {
        await using var disabled = Create(enabled: false, alias: true);
        await disabled.StartAsync();
        Assert.Empty(Routes(disabled));
        Assert.Empty(Probe(disabled).Materializations);
        Assert.Equal(0, Probe(disabled).Calls);
        await using var selected = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Configuration["Sample:ConventionAliases"] = "true";
        });
        await using var original = ExampleProgram.Build(configure: builder => builder.WebHost.UseTestServer());
        await selected.StartAsync();
        await original.StartAsync();
        Assert.Equal(4, Routes(selected).Count());
        Assert.DoesNotContain(Routes(original), endpoint => endpoint.RoutePattern.RawText!.StartsWith("/named-conventions", StringComparison.Ordinal));
        using var client = selected.GetTestClient();
        using var response = await client.GetAsync("/named-conventions/value");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("value", (await response.Content.ReadFromJsonAsync<AliasConventionReply>())!.Kind);
    }

    [Fact]
    public async Task Adapter_method_reflection_and_named_aliases_preserve_the_original_inherited_sequence()
    {
        var original = typeof(GenericAliasRoutes.GenericOverriddenEndpoint).GetMethod("HandleAsync")!;
        var named = typeof(NamedAliases.GetNamedConventionOverriddenEndpoint).GetMethod("HandleAsync")!;
        var originalDeclarations = original.GetCustomAttributes(inherit: true).OfType<EndpointConventionAttribute>().ToArray();
        var namedDeclarations = named.GetCustomAttributes(inherit: true).OfType<EndpointConventionAttribute>().ToArray();
        Assert.Equal(originalDeclarations.Select(ConventionTarget), namedDeclarations.Select(ConventionTarget));
        foreach (var item in new[] { (Reference: "generic", Method: original, Declarations: originalDeclarations), (Reference: "named", Method: named, Declarations: namedDeclarations) })
        {
            var typed = item.Method.GetCustomAttributes<EndpointConventionAttribute>(inherit: true).ToArray();
            var staticallyRead = Attribute.GetCustomAttributes(item.Method, typeof(EndpointConventionAttribute), inherit: true).Cast<EndpointConventionAttribute>().ToArray();
            await using var app = Create(item.Reference);
            await app.StartAsync();
            var actual = Stages(Route(app, "/named-conventions/overridden")).Count(stage => stage == "method");
            Assert.All(item.Declarations, declaration => Assert.Equal(typeof(AliasMethodConvention), ConventionTarget(declaration)));
            Assert.Equal(item.Declarations.Length, actual);
            output.WriteLine("owner={0}; base={1}; broad={2}; typed={3}; static={4}; materializedMethodStages={5}; targets={6}", item.Reference, item.Method.GetBaseDefinition().DeclaringType!.FullName, item.Declarations.Length, typed.Length, staticallyRead.Length, actual, string.Join(",", item.Declarations.Select(declaration => ConventionTarget(declaration).FullName)));
            foreach (var type in item.Declarations.Concat(typed).Concat(staticallyRead).Select(declaration => declaration.GetType()).Distinct())
            {
                output.WriteLine("concrete={0}; directUsage={1}; inheritedUsage={2}", type.AssemblyQualifiedName, Usage(type.GetCustomAttribute<AttributeUsageAttribute>(inherit: false)), Usage(type.GetCustomAttribute<AttributeUsageAttribute>(inherit: true)));
            }
        }
    }

    private static Type ConventionTarget(EndpointConventionAttribute declaration)
    {
        for (var type = declaration.GetType(); type is not null; type = type.BaseType)
        {
            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(EndpointConventionAttribute<>)) { return type.GenericTypeArguments[0]; }
        }

        throw new InvalidOperationException("The declaration has no fixed generic convention target.");
    }

    private static string Usage(AttributeUsageAttribute? usage)
    {
        return usage is null ? "<absent>" : "targets=" + usage.ValidOn + ",multiple=" + usage.AllowMultiple + ",inherited=" + usage.Inherited;
    }

    private static Type AliasType(string name)
    {
        return typeof(AliasConventionStage).Assembly.GetType(typeof(AliasConventionStage).Namespace + "." + name, throwOnError: true)!;
    }

    private static string[] Paths()
    {
        return new[] { "/named-conventions/value", "/named-conventions/repeated", "/named-conventions/inherited", "/named-conventions/overridden" };
    }

    private static string[] Stages(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<AliasConventionStage>().Select(stage => stage.Value).ToArray();
    }

    private static string[] Headers(HttpResponseMessage response)
    {
        return response.Headers.Where(pair => pair.Key.StartsWith("X-Alias-", StringComparison.OrdinalIgnoreCase)).Select(pair => pair.Key + ":" + string.Join("|", pair.Value)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private static async Task<HttpResponseMessage> Reject(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/named-conventions/value");
        request.Headers.Add("X-Reject", "true");
        return await client.SendAsync(request);
    }

    private static async Task SameResponse(HttpResponseMessage expected, HttpResponseMessage actual)
    {
        Assert.Equal(expected.StatusCode, actual.StatusCode);
        Assert.Equal(expected.Content.Headers.ContentType?.ToString(), actual.Content.Headers.ContentType?.ToString());
        Assert.Equal(Headers(expected), Headers(actual));
        Assert.Equal(await expected.Content.ReadAsByteArrayAsync(), await actual.Content.ReadAsByteArrayAsync());
    }

    private static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
internal sealed class UniqueNamedAttribute : EndpointConventionAttribute<UniqueUsageConvention>
{
    public UniqueNamedAttribute(string owner)
    {
        Owner = owner;
    }

    public string Owner { get; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
internal sealed class LocalNamedAttribute : EndpointConventionAttribute<LocalUsageConvention> { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
internal sealed class RepeatedNamedAttribute : EndpointConventionAttribute<RepeatedUsageConvention> { }

internal sealed class UniqueUsageConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.Add(builder => builder.Metadata.Add(new AliasConventionStage("usage-unique")));
    }
}

internal sealed class LocalUsageConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.Add(builder => builder.Metadata.Add(new AliasConventionStage("usage-local")));
    }
}

internal sealed class RepeatedUsageConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.Add(builder => builder.Metadata.Add(new AliasConventionStage("usage-repeat")));
    }
}

internal sealed class BaseUsageConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder endpoint)
    {
        endpoint.Add(builder => builder.Metadata.Add(new AliasConventionStage("usage-class-base")));
    }
}

[EndpointConvention<BaseUsageConvention>]
internal abstract class NamedUsageEndpointBase : IGetEndpoint, IManuallyMappedEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/named-usage";

    [UniqueNamed("base")]
    [LocalNamed]
    [RepeatedNamed]
    public virtual Ok<string> HandleAsync()
    {
        return TypedResults.Ok("usage");
    }
}

internal sealed class NamedUsageDerivedEndpoint : NamedUsageEndpointBase
{
    [UniqueNamed("derived")]
    [RepeatedNamed]
    public override Ok<string> HandleAsync()
    {
        return TypedResults.Ok("usage");
    }
}