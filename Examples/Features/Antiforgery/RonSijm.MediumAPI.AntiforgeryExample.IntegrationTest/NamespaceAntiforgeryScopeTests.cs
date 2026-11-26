using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.AntiforgeryExample.Endpoints.Forms;
using RonSijm.MediumAPI.AntiforgeryExample.Endpoints.Forms.DeclaredFalse;
using RonSijm.MediumAPI.AntiforgeryExample.Endpoints.Forms.FluentDisabled;
using RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.Fixtures.Disabled;
using RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.Fixtures.Disabled.Child;
using RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.Fixtures.Ignored;
using RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.Fixtures.MetadataOnly;
using RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.Fixtures.Ordered;
using Xunit;
using static RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest.AntiforgeryHost;

namespace RonSijm.MediumAPI.AntiforgeryExample.IntegrationTest;

public sealed class NamespaceAntiforgeryScopeTests
{
    [Fact]
    public async Task Ordered_metadata_preserves_Map_class_callback_handler_and_endpoint_configuration_boundaries()
    {
        await using var native = Create(mapEndpoints: false);
        await using var generated = Create(mapEndpoints: false);
        var nativeGroup = native.MapGroup("/ordered").WithMetadata(new RequireAntiforgeryTokenAttribute(true), new StageMarker("Map"));
        nativeGroup.WithMetadata(new RequireAntiforgeryTokenAttribute(false));
        nativeGroup.WithMetadata(new RequireAntiforgeryTokenAttribute(true), new StageMarker("Callback"));
        nativeGroup.MapGet("/value", NativeOrdered).WithMetadata(new RequireAntiforgeryTokenAttribute(false), new StageMarker("Endpoint")).WithName("Ordered");
        generated.MapMediumApiNamespace<OrderedGroupDefinition>(group => group.WithMetadata(new RequireAntiforgeryTokenAttribute(true), new StageMarker("Callback")));
        await native.StartAsync();
        await generated.StartAsync();
        Assert.Equal(MetadataInventory(native), MetadataInventory(generated));
        var metadata = Route(generated, "/ordered/value").Metadata;
        var attributes = metadata.GetOrderedMetadata<RequireAntiforgeryTokenAttribute>();
        Assert.Equal(new[] { true, false, true, true, false }, attributes.Select(attribute => attribute.RequiresValidation));
        var ordered = metadata.ToList();
        Assert.True(ordered.FindIndex(value => ReferenceEquals(value, attributes[1])) < ordered.FindIndex(value => value is StageMarker { Stage: "Callback" }));
        Assert.True(ordered.FindIndex(value => value is StageMarker { Stage: "Callback" }) < ordered.FindIndex(value => ReferenceEquals(value, attributes[3])));
        Assert.True(ordered.FindIndex(value => ReferenceEquals(value, attributes[3])) < ordered.FindIndex(value => value is StageMarker { Stage: "Endpoint" }));
        using var client = generated.GetTestClient();
        Assert.Equal(new FixtureReceipt("ordered", 1, null), await client.GetFromJsonAsync<FixtureReceipt>("/ordered/value"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_typed_callback_keeps_native_metadata_vs_DisableAntiforgery_Finally_behavior(bool disableFinally)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            if (native)
            {
                var forms = app.MapGroup("/forms").WithMetadata(new RequireAntiforgeryTokenAttribute());
                if (disableFinally)
                {
                    forms.DisableAntiforgery();
                }
                else
                {
                    forms.WithMetadata(new RequireAntiforgeryTokenAttribute(false));
                }

                forms.MapPost("/required", ([FromForm] string message, FormProbe probe) => NativeForm(message, "required", probe));
                forms.MapGroup("/declared-false").WithMetadata(new RequireAntiforgeryTokenAttribute(false)).MapPost("/method-required", NativeRequiredForm);
            }
            else
            {
                app.MapMediumApiNamespace<FormsGroupDefinition>(group =>
                {
                    if (disableFinally)
                    {
                        group.DisableAntiforgery();
                    }
                    else
                    {
                        group.WithMetadata(new RequireAntiforgeryTokenAttribute(false));
                    }
                });
            }

            await app.StartAsync();
            using var client = app.GetTestClient();
            foreach (var (path, handler) in new[] { ("/forms/required", "required"), ("/forms/declared-false/method-required", "method-required") })
            {
                Assert.Equal(!disableFinally, Route(app, path).Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
                using var response = await Post(client, path);
                if (disableFinally)
                {
                    await AssertReceipt(response, handler);
                }
                else
                {
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                }

                Assert.Equal(disableFinally ? 1 : 0, Probe(app).CallsFor(handler));
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Direct_child_mounts_use_only_their_own_prefix_and_declarations(bool fluentDisabled)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            var path = fluentDisabled ? "/fluent-disabled/method-required" : "/declared-false/inferred";
            if (native)
            {
                if (fluentDisabled)
                {
                    app.MapGroup("/fluent-disabled").DisableAntiforgery().WithMetadata(new RequireAntiforgeryTokenAttribute()).MapPost("/method-required", NativeFluentForm);
                }
                else
                {
                    app.MapGroup("/declared-false").WithMetadata(new RequireAntiforgeryTokenAttribute(false)).MapPost("/inferred", ([FromForm] string message, FormProbe probe) => NativeForm(message, "inferred", probe));
                }
            }
            else if (fluentDisabled)
            {
                app.MapMediumApiNamespace<FluentDisabledGroupDefinition>();
            }
            else
            {
                app.MapMediumApiNamespace<DeclaredFalseGroupDefinition>();
            }

            await app.StartAsync();
            var declarations = Route(app, path).Metadata.GetOrderedMetadata<RequireAntiforgeryTokenAttribute>();
            Assert.Equal(fluentDisabled ? new[] { true, true } : new[] { false }, declarations.Select(attribute => attribute.RequiresValidation));
            Assert.Equal(!fluentDisabled, Route(app, path).Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
            Assert.DoesNotContain(Routes(app), endpoint => endpoint.RoutePattern.RawText!.StartsWith("/antiforgery", StringComparison.Ordinal) || endpoint.RoutePattern.RawText.StartsWith("/forms", StringComparison.Ordinal));
            using var client = app.GetTestClient();
            using var response = await Post(client, path);
            if (fluentDisabled)
            {
                await AssertReceipt(response, "fluent-disabled");
            }
            else
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }

            Assert.Equal(fluentDisabled ? 1 : 0, Probe(app).TotalCalls);
        }
    }

    [Fact]
    public async Task Repeated_mounts_and_independent_hosts_receive_fresh_namespace_attribute_instances()
    {
        await using var repeated = Create(mapEndpoints: false);
        await using var fresh = Create();
        repeated.MapGroup("/first").MapMediumApiNamespace<FormsGroupDefinition>(group => group.WithoutDefaultEndpointName());
        repeated.MapGroup("/second").MapMediumApiNamespace<FormsGroupDefinition>(group => group.WithoutDefaultEndpointName());
        await repeated.StartAsync();
        await fresh.StartAsync();
        var first = Assert.Single(Route(repeated, "/first/forms/required").Metadata.GetOrderedMetadata<RequireAntiforgeryTokenAttribute>());
        var second = Assert.Single(Route(repeated, "/second/forms/required").Metadata.GetOrderedMetadata<RequireAntiforgeryTokenAttribute>());
        var independent = Assert.Single(Route(fresh, FormRoot + "/required").Metadata.GetOrderedMetadata<RequireAntiforgeryTokenAttribute>());
        Assert.NotSame(first, second);
        Assert.NotSame(first, independent);
        Assert.NotSame(second, independent);
        using var client = repeated.GetTestClient();
        foreach (var prefix in new[] { "/first", "/second" })
        {
            using var response = await Post(client, prefix + "/forms/required");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        Assert.Equal(0, Probe(repeated).TotalCalls);
        Assert.Equal(0, Probe(fresh).TotalCalls);
    }

    [Fact]
    public async Task Inherited_derived_and_unrelated_antiforgery_attributes_are_ignored_without_construction()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<IgnoredGroupDefinition>();
        await app.StartAsync();
        var metadata = Route(app, "/ignored/value").Metadata;
        Assert.Null(metadata.GetMetadata<IAntiforgeryMetadata>());
        Assert.DoesNotContain(metadata, value => value is DerivedAntiforgeryAttribute or UnrelatedAntiforgeryAttribute);
        using var client = app.GetTestClient();
        Assert.Equal(new FixtureReceipt("ignored", 1, null), await client.GetFromJsonAsync<FixtureReceipt>("/ignored/value"));
    }

    [Fact]
    public async Task Disabled_parents_skip_mapping_descendant_conditions_metadata_and_typed_callbacks()
    {
        await using var app = Create(mapEndpoints: false);
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        await app.StartAsync();
        Assert.Empty(Routes(app));
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/disabled/value");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, app.Services.GetRequiredService<FixtureProbe>().CallsFor("disabled"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Metadata_on_a_nonform_handler_preserves_native_feature_reporting_without_a_blanket_rejection(bool native)
    {
        await using var app = Create(mapEndpoints: false);
        if (native)
        {
            MapNativeMetadataOnly(app);
        }
        else
        {
            app.MapMediumApiNamespace<MetadataOnlyGroupDefinition>();
        }

        await app.StartAsync();
        Assert.True(Route(app, "/metadata-only/value").Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        using var client = app.GetTestClient();
        using var response = await client.PostAsync("/metadata-only/value", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new FixtureReceipt("metadata-only", 1, false), await response.Content.ReadFromJsonAsync<FixtureReceipt>());
        Assert.Equal(new FixtureReceipt("safe", 1, null), await client.GetFromJsonAsync<FixtureReceipt>("/metadata-only/safe"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Applying_metadata_does_not_register_services_or_install_native_middleware(bool native)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(NamespaceAntiforgeryScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<FixtureProbe>();
        await using var app = builder.Build();
        if (native)
        {
            MapNativeMetadataOnly(app);
        }
        else
        {
            app.MapMediumApiNamespace<MetadataOnlyGroupDefinition>();
        }

        Assert.Null(app.Services.GetService<IAntiforgery>());
        Assert.True(Route(app, "/metadata-only/value").Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        Assert.Throws<InvalidOperationException>(() => app.UseAntiforgery());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Registered_native_services_still_require_the_host_to_install_middleware(bool native)
    {
        await using var app = Create(native, useAntiforgery: false);
        await app.StartAsync();
        Assert.NotNull(app.Services.GetRequiredService<IAntiforgery>());
        using var client = app.GetTestClient();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using var response = await Post(client, FormRoot + "/required");
        });
        Assert.Contains("antiforgery", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, Probe(app).TotalCalls);
    }

    private static void MapNativeMetadataOnly(WebApplication app)
    {
        var group = app.MapGroup("/metadata-only").WithMetadata(new RequireAntiforgeryTokenAttribute());
        group.MapPost("/value", (HttpContext context, FixtureProbe probe) => TypedResults.Ok(probe.Record("metadata-only", context.Features.Get<IAntiforgeryValidationFeature>()?.IsValid))).WithName("PostMetadataOnly");
        group.MapGet("/safe", (HttpContext context, FixtureProbe probe) => TypedResults.Ok(probe.Record("safe", context.Features.Get<IAntiforgeryValidationFeature>()?.IsValid))).WithName("GetMetadataOnly");
    }

    private static Ok<FormReceipt> NativeForm(string message, string handler, FormProbe probe)
    {
        probe.Record(handler);
        return TypedResults.Ok(new FormReceipt(message, handler));
    }

    [RequireAntiforgeryToken(true)]
    private static Ok<FormReceipt> NativeRequiredForm([FromForm] string message, FormProbe probe) => NativeForm(message, "method-required", probe);

    [RequireAntiforgeryToken(true)]
    private static Ok<FormReceipt> NativeFluentForm([FromForm] string message, FormProbe probe) => NativeForm(message, "fluent-disabled", probe);

    [RequireAntiforgeryToken(true)]
    private static Ok<FixtureReceipt> NativeOrdered(FixtureProbe probe) => TypedResults.Ok(probe.Record("ordered"));
}