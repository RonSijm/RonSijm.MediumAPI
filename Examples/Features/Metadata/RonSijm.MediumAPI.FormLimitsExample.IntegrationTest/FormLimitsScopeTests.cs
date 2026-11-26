using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Composed.Child;
using RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Disabled;
using RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Disabled.Child;
using RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Forms;
using RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Fresh;
using RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Ignored;
using RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Inheritance;
using RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Manual;
using RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Ordered;
using RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Scoped;
using RonSijm.MediumAPI.FormLimitsExample.IntegrationTest.ScopeFixtures.Snapshot;
using Xunit;

namespace RonSijm.MediumAPI.FormLimitsExample.IntegrationTest;

public sealed class FormLimitsScopeTests
{
    [Fact]
    public async Task Ordered_metadata_retains_Map_definition_callback_class_provider_method_Configure_and_native_final_conventions()
    {
        await using var native = Create();
        await using var generated = Create();
        var nativeGroup = native.MapGroup("/scope/ordered").WithMetadata(new RequestFormLimitsAttribute { ValueCountLimit = 1000 });
        ((IEndpointConventionBuilder)nativeGroup).Finally(builder => builder.Metadata.Add(new RequestFormLimitsAttribute { ValueCountLimit = 1800 }));
        nativeGroup.WithMetadata(new RequestFormLimitsAttribute { ValueCountLimit = 1100, Order = int.MaxValue }, new RequestFormLimitsAttribute { ValueCountLimit = 1200 });
        var nativeClass = nativeGroup.MapGroup("").WithMetadata(new RequestFormLimitsAttribute { ValueCountLimit = 1300, Order = int.MinValue });
        var nativeEndpoint = nativeClass.MapGet("/value", NativeOrdered).WithName("Ordered").WithMetadata(new RequestFormLimitsAttribute { ValueCountLimit = 1600 });
        nativeEndpoint.Finally(builder => builder.Metadata.Add(new RequestFormLimitsAttribute { ValueCountLimit = 1700 }));
        generated.MapMediumApiNamespace<OrderedGroupDefinition>(group => group.WithMetadata(new RequestFormLimitsAttribute { ValueCountLimit = 1200 })).MapMediumApiEndpoint<OrderedEndpoint>();
        var route = Route(generated, "/scope/ordered/value");
        Assert.Equal(Enumerable.Range(10, 9).Select(value => (int?)(value * 100)), Counts(route));
        Assert.Equal(Counts(Route(native, "/scope/ordered/value")), Counts(route));
        Assert.Equal(1800, route.Metadata.GetMetadata<IFormOptionsMetadata>()!.ValueCountLimit);
        Assert.Equal(typeof(OrderedEndpoint), route.Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        Assert.Equal(typeof(OrderedEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        await native.StartAsync();
        await generated.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        foreach (var app in new[] { native, generated })
        {
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/scope/ordered/value");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact]
    public async Task CLR_base_class_attributes_are_ignored_while_original_inherited_handler_metadata_is_retained()
    {
        await using var app = Create();
        app.MapMediumApiNamespace<BaseGroupIgnoredDefinition>().MapMediumApiEndpoint<BaseGroupValueEndpoint>();
        app.MapMediumApiEndpoint<BaseClassIgnoredEndpoint>();
        app.MapMediumApiEndpoint<InheritedMethodEndpoint>();
        Assert.Empty(Counts(Route(app, "/scope/base-group/value")));
        Assert.Empty(Counts(Route(app, "/scope/base-class-ignored")));
        var route = Route(app, "/scope/inherited-method");
        Assert.Equal(new int?[] { 1300, 1500 }, Counts(route));
        Assert.Equal(typeof(BaseMethodEndpoint), route.Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        Assert.Equal(typeof(InheritedMethodEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
        Assert.Equal(typeof(InheritedMethodEndpoint), Assert.Single(app.GetMediumApiRouteReport().Routes, value => value.Pattern == "/scope/inherited-method").EndpointType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/scope/base-group/value", "/scope/base-class-ignored", "/scope/inherited-method" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact]
    public async Task Only_the_exact_native_type_is_forwarded_without_constructing_subclasses_or_arbitrary_form_metadata()
    {
        Assert.False(typeof(RequestFormLimitsAttribute).IsSealed);
        await using var app = Create();
        app.MapMediumApiNamespace<IgnoredGroupDefinition>().MapMediumApiEndpoint<IgnoredEndpoint>();
        var metadata = Route(app, "/scope/ignored/value").Metadata;
        Assert.Equal(new int?[] { 1000, 1100 }, metadata.GetOrderedMetadata<IFormOptionsMetadata>().Select(value => value.ValueCountLimit));
        Assert.DoesNotContain(metadata, value => value is DerivedFormLimitsAttribute or UnrelatedFormOptionsAttribute);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/ignored/value");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Repeated_mounts_and_hosts_get_fresh_mutable_native_attributes_without_activating_request_targets()
    {
        await using var repeated = Create();
        await using var independent = Create();
        repeated.MapGroup("/first").MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>().WithName("FirstFresh");
        repeated.MapGroup("/second").MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>().WithName("SecondFresh");
        independent.MapMediumApiNamespace<FreshGroupDefinition>().MapMediumApiEndpoint<FreshEndpoint>();
        var first = Route(repeated, "/first/scope/fresh/value").Metadata.GetOrderedMetadata<RequestFormLimitsAttribute>();
        var second = Route(repeated, "/second/scope/fresh/value").Metadata.GetOrderedMetadata<RequestFormLimitsAttribute>();
        var other = Route(independent, "/scope/fresh/value").Metadata.GetOrderedMetadata<RequestFormLimitsAttribute>();
        Assert.Equal(2, first.Count);
        Assert.Equal(2, second.Count);
        Assert.Equal(2, other.Count);
        for (var index = 0; index < 2; index++)
        {
            Assert.NotSame(first[index], second[index]);
            Assert.NotSame(first[index], other[index]);
            first[index].ValueCountLimit = 17;
            Assert.Equal(index == 0 ? 1000 : 1300, second[index].ValueCountLimit);
            Assert.Equal(index == 0 ? 1000 : 1300, other[index].ValueCountLimit);
        }

        Assert.Equal(2, repeated.GetMediumApiRouteReport().Routes.Count);
        Assert.Equal(2, repeated.GetMediumApiContractSnapshot().Routes.Count);
        await repeated.StartAsync();
        Assert.Equal(2, Descriptions(repeated).Operations.Count);
        // The mapped constructor throws: metadata, reports, API descriptions and startup must not activate it.
    }

    [Fact]
    public async Task Manifests_and_automatic_selection_skip_manual_and_disabled_targets_without_constructing_supported_metadata()
    {
        await using var app = Create();
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Contains(manifest.Endpoints, value => value.EndpointType == typeof(ManualThrowingEndpoint) && value.IsManuallyMapped);
        Assert.Contains(manifest.Endpoints, value => value.EndpointType == typeof(DisabledEndpoint));
        Assert.Contains(manifest.Endpoints, value => value.EndpointType == typeof(DisabledChildEndpoint));
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Empty(Routes(app));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope/disabled");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, app.Services.GetRequiredService<ScopeFormProbe>().Calls);
    }

    [Fact]
    public async Task Disabled_typed_mounts_skip_callbacks_child_conditions_and_form_metadata()
    {
        await using var app = Create();
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callbackInvoked = true));
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.False(callbackInvoked);
        Assert.Empty(Routes(app));
    }

    [Fact]
    public async Task Explicit_aliases_keep_class_limits_without_importing_ancestor_prefixes_policies_or_conditions()
    {
        await using var app = Create();
        app.MapMediumApiNamespace<ScopedGroupDefinition>().MapMediumApiEndpoint<ScopedEndpoint>();
        app.MapMediumApiEndpoint<ScopedEndpoint>("/scope/alias").WithName("ScopedAlias");
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        app.MapMediumApiEndpoint<DisabledAliasEndpoint>("/scope/disabled-alias").WithName("DisabledAlias");
        Assert.Equal(new int?[] { 1000, 1300 }, Counts(Route(app, "/scope/scoped/value")));
        Assert.Equal(new int?[] { 1300 }, Counts(Route(app, "/scope/alias")));
        Assert.Equal(new int?[] { 1400 }, Counts(Route(app, "/scope/disabled-alias")));
        await app.StartAsync();
        using var client = app.GetTestClient();
        foreach (var path in new[] { "/scope/scoped/value", "/scope/alias", "/scope/disabled-alias" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact]
    public async Task Direct_child_mounts_do_not_reconstruct_ancestor_prefixes_or_form_limits()
    {
        await using var app = Create();
        app.MapMediumApiNamespace<ChildGroupDefinition>().MapMediumApiEndpoint<ChildEndpoint>();
        var route = Assert.Single(Routes(app));
        Assert.Equal("/scope-child/value", route.RoutePattern.RawText);
        Assert.Equal(new int?[] { 1200 }, Counts(route));
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/scope-child/value");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var ancestorPath = await client.GetAsync("/scope-parent/scope-child/value");
        Assert.Equal(HttpStatusCode.NotFound, ancestorPath.StatusCode);
    }

    [Fact]
    public async Task Native_attribute_defaults_reset_prior_options_while_sparse_fluent_values_preserve_others()
    {
        IFormOptionsMetadata attribute = new RequestFormLimitsAttribute { ValueCountLimit = 2 };
        var defaults = new FormOptions();
        foreach (var property in typeof(IFormOptionsMetadata).GetProperties())
        {
            var value = property.GetValue(attribute);
            Assert.NotNull(value);
            Assert.Equal(property.Name == nameof(IFormOptionsMetadata.ValueCountLimit) ? 2 : typeof(FormOptions).GetProperty(property.Name)!.GetValue(defaults), value);
        }

        foreach (var native in new[] { true, false })
        {
            await using var app = Create();
            var group = app.MapGroup("/defaults").WithFormOptions(valueCountLimit: 1, valueLengthLimit: 1);
            if (native)
            {
                Func<IFormCollection, HttpContext, ScopeFormProbe, CancellationToken, Task<Ok<ScopeFormResponse>>> handler = new ResetFormEndpoint().HandleAsync;
                group.MapPost("/reset", handler).WithName("Reset").WithMetadata(new RequestFormLimitsAttribute { ValueCountLimit = 2 }).DisableAntiforgery();
                group.MapPost("/sparse", handler).WithName("Sparse").WithMetadata(new RequestFormLimitsAttribute { ValueCountLimit = 2 }).WithFormOptions(valueLengthLimit: 1).DisableAntiforgery();
            }
            else
            {
                group.MapMediumApiEndpoint<ResetFormEndpoint>("/reset").WithName("Reset");
                group.MapMediumApiEndpoint<ResetFormEndpoint>("/sparse").WithName("Sparse").WithFormOptions(valueLengthLimit: 1);
            }

            var sparse = Route(app, "/defaults/sparse").Metadata.GetOrderedMetadata<IFormOptionsMetadata>().Last();
            Assert.Equal(1, sparse.ValueLengthLimit);
            Assert.Null(sparse.ValueCountLimit);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var accepted = await Post(client, "/defaults/reset", 2, "long");
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            using var rejected = await Post(client, "/defaults/sparse", 2, "long");
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.Equal(1, app.Services.GetRequiredService<ScopeFormProbe>().Calls);
        }
    }

    [Fact]
    public async Task Default_form_feature_captures_the_endpoint_when_created_and_early_content_type_checks_retain_native_timing()
    {
        foreach (var native in new[] { true, false })
        {
            foreach (var earlyFeature in new[] { false, true })
            {
                await using var app = Create(earlyFeature
                    ? application => application.Use(async (context, next) =>
                    {
                        Assert.Null(context.GetEndpoint());
                        Assert.True(context.Request.HasFormContentType);
                        await next(context);
                    })
                    : null);
                MapBoundForm(app, native);
                Assert.Equal(new int?[] { 1 }, Counts(Route(app, "/scope/form")));
                await app.StartAsync();
                using var client = app.GetTestClient();
                using var response = await Post(client, "/scope/form", 2);
                Assert.Equal(earlyFeature ? HttpStatusCode.OK : HttpStatusCode.BadRequest, response.StatusCode);
                if (earlyFeature)
                {
                    Assert.Equal(new ScopeFormResponse(2, "posted", true), await response.Content.ReadFromJsonAsync<ScopeFormResponse>());
                }

                Assert.Equal(earlyFeature ? 1 : 0, app.Services.GetRequiredService<ScopeFormProbe>().Calls);
            }
        }
    }

    [Fact]
    public async Task Public_custom_FormFeature_uses_its_explicit_options_without_retroactively_importing_endpoint_metadata()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(application => application.Use(async (context, next) =>
            {
                context.Features.Set<IFormFeature>(new FormFeature(context.Request, new FormOptions { ValueCountLimit = 3 }));
                await next(context);
            }));
            MapBoundForm(app, native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var accepted = await Post(client, "/scope/form", 2);
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            Assert.Equal(new ScopeFormResponse(2, "posted", true), await accepted.Content.ReadFromJsonAsync<ScopeFormResponse>());
            using var rejected = await Post(client, "/scope/form", 4);
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.Equal(1, app.Services.GetRequiredService<ScopeFormProbe>().Calls);
        }
    }

    [Fact]
    public async Task Cached_native_form_collections_are_reused_without_reparsing_or_enforcing_new_endpoint_metadata()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(application => application.Use(async (context, next) =>
            {
                context.Request.Form = new FormCollection(new Dictionary<string, StringValues> { ["first"] = "cached", ["second"] = "cached" });
                await next(context);
            }));
            MapBoundForm(app, native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Post(client, "/scope/form", 4);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new ScopeFormResponse(2, "cached", true), await response.Content.ReadFromJsonAsync<ScopeFormResponse>());
            Assert.Equal(1, app.Services.GetRequiredService<ScopeFormProbe>().Calls);
        }
    }

    [Fact]
    public async Task Raw_and_API_snapshots_keep_native_contracts_without_adding_form_option_fields()
    {
        await using var native = Create();
        await using var generated = Create();
        Func<NoContent> handler = new SnapshotEndpoint().HandleAsync;
        native.MapPost("/scope/snapshot", handler).WithName("Snapshot").WithMetadata(new RequestFormLimitsAttribute { ValueCountLimit = 64, ValueLengthLimit = 32 });
        generated.MapMediumApiEndpoint<SnapshotEndpoint>();
        await native.StartAsync();
        await generated.StartAsync();
        var snapshot = generated.GetMediumApiContractSnapshot();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), snapshot));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.DoesNotContain("valueCountLimit", snapshot.ToJson(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("multipartBodyLengthLimit", snapshot.ToJson(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("valueCountLimit", Descriptions(generated).ToJson(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new int?[] { 64 }, Counts(Route(generated, "/scope/snapshot")));
        Assert.Equal(typeof(SnapshotEndpoint), Assert.Single(generated.GetMediumApiRouteReport().Routes).EndpointType);
    }

    [Fact]
    public async Task Form_limit_metadata_preserves_native_inferred_antiforgery_requirements_and_missing_middleware_errors()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(configure: builder => builder.Services.AddAntiforgery());
            if (native)
            {
                Func<IFormCollection, HttpContext, ScopeFormProbe, CancellationToken, Task<Ok<ScopeFormResponse>>> handler = new ValidatedFormEndpoint().HandleAsync;
                app.MapPost("/scope/validated-form", handler).WithName("ValidatedForm").WithMetadata(new RequestFormLimitsAttribute { ValueCountLimit = 1 });
            }
            else
            {
                app.MapMediumApiEndpoint<ValidatedFormEndpoint>();
            }

            var metadata = Route(app, "/scope/validated-form").Metadata;
            Assert.True(metadata.GetMetadata<Microsoft.AspNetCore.Antiforgery.IAntiforgeryMetadata>()!.RequiresValidation);
            Assert.Equal(1, metadata.GetMetadata<IFormOptionsMetadata>()!.ValueCountLimit);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                using var response = await Post(client, "/scope/validated-form", 1);
            });
            Assert.Contains("antiforgery", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, app.Services.GetRequiredService<ScopeFormProbe>().Calls);
        }
    }

    private static WebApplication Create(Action<WebApplication>? beforeRouting = null, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(FormLimitsScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<ScopeFormProbe>();
        configure?.Invoke(builder);
        var app = builder.Build();
        beforeRouting?.Invoke(app);
        app.UseRouting();
        return app;
    }

    private static void MapBoundForm(WebApplication app, bool native)
    {
        if (native)
        {
            Func<IFormCollection, HttpContext, ScopeFormProbe, CancellationToken, Task<Ok<ScopeFormResponse>>> handler = new BoundFormEndpoint().HandleAsync;
            app.MapPost("/scope/form", handler).WithName("BoundForm").WithMetadata(new RequestFormLimitsAttribute { ValueCountLimit = 1 }).DisableAntiforgery();
        }
        else
        {
            app.MapMediumApiEndpoint<BoundFormEndpoint>();
        }
    }

    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, int count, string value = "posted")
    {
        var fields = Enumerable.Range(0, count).Select(index => new KeyValuePair<string, string>(index == 0 ? "first" : "field" + index, value));
        using var body = new FormUrlEncodedContent(fields);
        return await client.PostAsync(path, body);
    }

    private static IEnumerable<RouteEndpoint> Routes(WebApplication app) => ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    private static RouteEndpoint Route(WebApplication app, string path) => Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    private static int?[] Counts(RouteEndpoint endpoint) => endpoint.Metadata.GetOrderedMetadata<IFormOptionsMetadata>().Select(value => value.ValueCountLimit).ToArray();
    private static MediumApiDescriptionSnapshot Descriptions(WebApplication app) => app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    private static void Equivalent(MediumApiContractComparison comparison) => Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));

    [RequestFormLimits(ValueCountLimit = 1500, Order = -1000)]
    private static ScopeFormResult NativeOrdered() => new();
}