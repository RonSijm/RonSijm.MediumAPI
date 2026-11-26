using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Composed.Child;
using RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Disabled;
using RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Disabled.Child;
using RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Inheritance;
using RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Manual;
using RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Ordered;
using RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Scoped;
using RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.ScopeFixtures.Selected;
using Xunit;
using static RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest.EndpointAntiforgeryHost;

namespace RonSijm.MediumAPI.EndpointAntiforgeryExample.IntegrationTest;

public sealed class EndpointAntiforgeryScopeTests
{
    [Fact]
    public async Task Class_metadata_precedes_inferred_provider_original_method_Configure_and_native_final_conventions()
    {
        await using var native = Create(mapEndpoints: false);
        await using var generated = Create(mapEndpoints: false);
        native.MapGroup("/scope/ordered").WithMetadata(new RequireAntiforgeryTokenAttribute(true), new RequireAntiforgeryTokenAttribute(false)).MapGet("/value", NativeOrdered).WithMetadata(new RequireAntiforgeryTokenAttribute(true)).DisableAntiforgery();
        generated.MapMediumApiNamespace<OrderedGroupDefinition>().MapMediumApiEndpoint<OrderedEndpoint>();
        var expected = new[] { true, false, true, false, true, false };
        Assert.Equal(expected, Route(native, "/scope/ordered/value").Metadata.GetOrderedMetadata<IAntiforgeryMetadata>().Select(metadata => metadata.RequiresValidation));
        var metadata = Route(generated, "/scope/ordered/value").Metadata;
        Assert.Equal(expected, metadata.GetOrderedMetadata<IAntiforgeryMetadata>().Select(attribute => attribute.RequiresValidation));
        Assert.False(metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        Assert.Equal(typeof(OrderedEndpoint), metadata.GetMetadata<MethodInfo>()!.DeclaringType);
    }

    [Fact]
    public async Task Only_exact_native_attributes_are_selected_while_derived_custom_and_MVC_attributes_remain_unconstructed()
    {
        Assert.False(typeof(RequireAntiforgeryTokenAttribute).IsSealed);
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<IgnoredAttributesEndpoint>();
        var metadata = Route(app, "/scope/ignored-attributes").Metadata;
        Assert.False(Assert.Single(metadata.GetOrderedMetadata<IAntiforgeryMetadata>()).RequiresValidation);
        Assert.IsType<RequireAntiforgeryTokenAttribute>(metadata.GetMetadata<IAntiforgeryMetadata>());
        Assert.DoesNotContain(metadata, value => value is DerivedAntiforgeryAttribute or UnrelatedAntiforgeryAttribute or UnrelatedMvcAttribute);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/ignored-attributes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, Probe(app).CallsFor("ignored-attributes"));
    }

    [Fact]
    public async Task CLR_base_class_attributes_are_ignored_but_inherited_original_method_attributes_and_arguments_remain()
    {
        await using var app = Create(mapEndpoints: false);
        var filterInvoked = false;
        app.MapMediumApiEndpoint<BaseClassIgnoredEndpoint>();
        app.MapMediumApiEndpoint<InheritedMethodEndpoint>().AddEndpointFilter(async (context, next) =>
        {
            Assert.Equal("synthetic-form-value", context.GetArgument<string>(0));
            Assert.Same(Probe(app), context.GetArgument<FormProbe>(1));
            filterInvoked = true;
            return await next(context);
        });
        Assert.Empty(Route(app, "/scope/base-class-ignored").Metadata.GetOrderedMetadata<IAntiforgeryMetadata>());
        var metadata = Route(app, "/scope/inherited-method").Metadata;
        Assert.Equal(new[] { true, false }, metadata.GetOrderedMetadata<RequireAntiforgeryTokenAttribute>().Select(attribute => attribute.RequiresValidation));
        Assert.False(metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        Assert.Equal(typeof(BaseMethodEndpoint), metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var plain = await client.GetAsync("/scope/base-class-ignored");
        Assert.Equal(HttpStatusCode.OK, plain.StatusCode);
        using var inherited = await Post(client, "/scope/inherited-method");
        await AssertReceipt(inherited, "inherited-method");
        Assert.True(filterInvoked);
    }

    [Fact]
    public async Task Repeated_mounts_and_independent_hosts_receive_fresh_true_and_false_attribute_instances()
    {
        await using var repeated = Create(mapEndpoints: false);
        await using var independent = Create(mapEndpoints: false);
        repeated.MapMediumApiEndpoint<FreshTrueEndpoint>("/scope/first-true").WithName("FirstTrue");
        repeated.MapMediumApiEndpoint<FreshTrueEndpoint>("/scope/second-true").WithName("SecondTrue");
        repeated.MapMediumApiEndpoint<FreshFalseEndpoint>("/scope/first-false").WithName("FirstFalse");
        repeated.MapMediumApiEndpoint<FreshFalseEndpoint>("/scope/second-false").WithName("SecondFalse");
        independent.MapMediumApiEndpoint<FreshTrueEndpoint>();
        independent.MapMediumApiEndpoint<FreshFalseEndpoint>();
        foreach (var (kind, expected) in new[] { ("true", true), ("false", false) })
        {
            var first = Assert.Single(Route(repeated, "/scope/first-" + kind).Metadata.GetOrderedMetadata<RequireAntiforgeryTokenAttribute>());
            var second = Assert.Single(Route(repeated, "/scope/second-" + kind).Metadata.GetOrderedMetadata<RequireAntiforgeryTokenAttribute>());
            var other = Assert.Single(Route(independent, "/scope/fresh-" + kind).Metadata.GetOrderedMetadata<RequireAntiforgeryTokenAttribute>());
            Assert.Equal(expected, first.RequiresValidation);
            Assert.Equal(first.RequiresValidation, second.RequiresValidation);
            Assert.Equal(first.RequiresValidation, other.RequiresValidation);
            Assert.NotSame(first, second);
            Assert.NotSame(first, other);
        }
    }

    [Fact]
    public async Task Invalid_native_form_binding_skips_the_handler_and_disposes_the_created_endpoint()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<ActivationEndpoint>();
        Assert.True(Route(app, "/scope/activation").Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        Assert.Equal(0, Probe(app).CallsFor("activation-construction"));
        await app.StartAsync();
        Assert.Equal(0, Probe(app).CallsFor("activation-construction"));
        using var client = app.GetTestClient();
        using var response = await Post(client, "/scope/activation");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, Probe(app).CallsFor("activation-construction"));
        Assert.Equal(1, Probe(app).CallsFor("activation-disposal"));
        Assert.Equal(0, Probe(app).CallsFor("activation"));
        Assert.Equal(2, Probe(app).TotalCalls);
    }

    [Fact]
    public async Task Manifests_and_disabled_or_manual_automatic_selection_do_not_materialize_class_metadata()
    {
        await using var app = Create(mapEndpoints: false);
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(ManualThrowingEndpoint) && endpoint.IsManuallyMapped);
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(DisabledEndpoint));
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(DisabledChildEndpoint));
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Empty(Routes(app));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/disabled");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, Probe(app).TotalCalls);
    }

    [Fact]
    public async Task Disabled_typed_mounts_skip_descendant_conditions_mapping_callbacks_and_class_metadata()
    {
        await using var app = Create(mapEndpoints: false);
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callbackInvoked = true));
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        Assert.Empty(Routes(app));
    }

    [Fact]
    public async Task Explicit_aliases_retain_class_requirements_without_importing_ancestor_disabling_or_conditions()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ScopedGroupDefinition>().MapMediumApiEndpoint<ScopedEndpoint>();
        app.MapMediumApiEndpoint<ScopedEndpoint>("/scope/alias").WithName("ScopedAlias");
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        app.MapMediumApiEndpoint<DisabledAliasEndpoint>("/scope/explicit-disabled-alias").WithName("ExplicitDisabledAlias");
        Assert.False(Route(app, "/scope/scoped/value").Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        Assert.True(Route(app, "/scope/alias").Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        Assert.True(Route(app, "/scope/explicit-disabled-alias").Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var scoped = await Post(client, "/scope/scoped/value");
        await AssertReceipt(scoped, "scoped");
        foreach (var path in new[] { "/scope/alias", "/scope/explicit-disabled-alias" })
        {
            using var response = await Post(client, path);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        Assert.Equal(1, Probe(app).CallsFor("scoped"));
        Assert.Equal(0, Probe(app).CallsFor("disabled-alias"));
    }

    [Fact]
    public async Task A_direct_child_mount_keeps_its_class_requirement_without_importing_the_parent_final_opt_out()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ChildGroupDefinition>().MapMediumApiEndpoint<ChildEndpoint>();
        var endpoint = Assert.Single(Routes(app));
        Assert.Equal("/scope-child/value", endpoint.RoutePattern.RawText);
        Assert.True(endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await Post(client, "/scope-child/value");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, Probe(app).TotalCalls);
    }

    [Fact]
    public async Task Missing_services_preserve_the_immediate_native_UseAntiforgery_registration_failure()
    {
        var errors = new List<InvalidOperationException>();
        foreach (var native in new[] { true, false })
        {
            await using var app = BareHost();
            app.UseRouting();
            MapNonForm(app, native, "GET");
            Assert.True(Route(app, "/scope/nonform-get").Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
            Assert.Null(app.Services.GetService<IAntiforgery>());
            errors.Add(Assert.Throws<InvalidOperationException>(() => { app.UseAntiforgery(); }));
        }

        Assert.Contains("antiforgery", errors[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(errors[0].GetType(), errors[1].GetType());
        Assert.Equal(errors[0].Message, errors[1].Message);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task Effective_true_requires_native_middleware_even_for_safe_or_nonform_handlers(string method)
    {
        var errors = new List<InvalidOperationException>();
        foreach (var native in new[] { true, false })
        {
            await using var app = BareHost(registerAntiforgery: true);
            app.UseRouting();
            MapNonForm(app, native, method);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var request = new HttpRequestMessage(new HttpMethod(method), "/scope/nonform-" + method.ToLowerInvariant());
            errors.Add(await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request)));
            Assert.Equal(0, Probe(app).TotalCalls);
        }

        Assert.Contains("antiforgery", errors[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(errors[0].GetType(), errors[1].GetType());
        Assert.Contains("UseAntiforgery", errors[1].Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Middleware_before_routing_preserves_the_native_missing_endpoint_validation_guard()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = BareHost(registerAntiforgery: true);
            app.UseAntiforgery();
            app.UseRouting();
            MapNonForm(app, native, "GET");
            await app.StartAsync();
            using var client = app.GetTestClient();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/scope/nonform-get"));
            Assert.Contains("antiforgery", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, Probe(app).TotalCalls);
        }
    }

    [Fact]
    public async Task Effective_false_on_a_nonform_handler_does_not_require_middleware_or_services()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = BareHost();
            app.UseRouting();
            if (native)
            {
                app.MapGroup("").WithMetadata(new RequireAntiforgeryTokenAttribute(false)).MapGet("/scope/fresh-false", (FormProbe probe) => Reply("synthetic", "fresh-false", probe));
            }
            else
            {
                app.MapMediumApiEndpoint<FreshFalseEndpoint>();
            }

            Assert.False(Route(app, "/scope/fresh-false").Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/scope/fresh-false");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, Probe(app).CallsFor("fresh-false"));
        }
    }

    private static WebApplication BareHost(bool registerAntiforgery = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(EndpointAntiforgeryScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<FormProbe>();
        if (registerAntiforgery)
        {
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
            builder.Services.AddAntiforgery();
        }

        return builder.Build();
    }

    private static void MapNonForm(WebApplication app, bool native, string method)
    {
        if (native)
        {
            var group = app.MapGroup("").WithMetadata(new RequireAntiforgeryTokenAttribute());
            if (method == "GET")
            {
                group.MapGet("/scope/nonform-get", (FormProbe probe) => Reply("synthetic", "nonform-get", probe));
            }
            else
            {
                group.MapPost("/scope/nonform-post", (FormProbe probe) => Reply("synthetic", "nonform-post", probe));
            }
        }
        else if (method == "GET")
        {
            app.MapMediumApiEndpoint<NonFormRequiredGetEndpoint>();
        }
        else
        {
            app.MapMediumApiEndpoint<NonFormRequiredPostEndpoint>();
        }
    }

    [RequireAntiforgeryToken(false)]
    private static ScopeMetadataResult NativeOrdered() => new();
}