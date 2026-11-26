using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.NativeResponsesExample.Api;
using RonSijm.MediumAPI.NativeResponsesExample.IntegrationTest.Fixtures.Custom;
using RonSijm.MediumAPI.NativeResponsesExample.IntegrationTest.Fixtures.Disabled;
using RonSijm.MediumAPI.NativeResponsesExample.IntegrationTest.Fixtures.Ignored;
using Xunit;
using static RonSijm.MediumAPI.NativeResponsesExample.IntegrationTest.NativeResponsesHost;

namespace RonSijm.MediumAPI.NativeResponsesExample.IntegrationTest;

public sealed class NativeResponseTests
{
    [Fact]
    public void Native_attributes_keep_their_own_interfaces_defaults_and_mutable_properties()
    {
        var statusOnly = new ProducesResponseTypeAttribute(202);
        Assert.Equal(typeof(void), statusOnly.Type);
        Assert.Equal(202, statusOnly.StatusCode);
        Assert.IsAssignableFrom<IApiResponseMetadataProvider>(statusOnly);
        Assert.False(typeof(IProducesResponseTypeMetadata).IsAssignableFrom(statusOnly.GetType()));
        var generic = new ProducesResponseTypeAttribute<DeclaredFailure>(404, "application/json", "application/vnd.catalog-error+json");
        Assert.Equal(typeof(DeclaredFailure), generic.Type);
        var media = new MediaTypeCollection();
        ((IApiResponseMetadataProvider)generic).SetContentTypes(media);
        Assert.Equal(new[] { "application/json", "application/vnd.catalog-error+json" }, media);
        generic.Type = typeof(GroupNotice);
        generic.StatusCode = 410;
        Assert.Equal(typeof(GroupNotice), generic.Type);
        Assert.Equal(410, generic.StatusCode);
    }

    [Fact]
    public async Task All_ordered_MVC_provider_values_match_independent_native_registration_without_running_handlers()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        await app.StartAsync();
        await native.StartAsync();
        Assert.Equal(12, Routes(app).Length);
        foreach (var route in Routes(app))
        {
            Assert.Equal(MvcValues(Route(native, route.RoutePattern.RawText!)), MvcValues(route));
        }

        Assert.Equal(0, Probe(app).HandlerCalls);
        Assert.Equal(0, Probe(app).FilterCalls);
        Assert.Equal(0, Probe(native).HandlerCalls);
    }

    [Fact]
    public async Task Real_API_Explorer_contracts_match_native_defaulting_and_provider_priority()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        await app.StartAsync();
        await native.StartAsync();
        Assert.Equal(12, DescriptionValues(app).Length);
        Assert.Equal(DescriptionValues(native), DescriptionValues(app));
        var defaults = Route(app, "/responses/defaults").Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>();
        Assert.Equal(typeof(void), Assert.Single(defaults, metadata => metadata.StatusCode == 202).Type);
        Assert.Equal(typeof(DeclaredFailure), Assert.Single(defaults, metadata => metadata.StatusCode == 422).Type);
        Assert.DoesNotContain(defaults, metadata => metadata.StatusCode == 409);
        Assert.Equal(0, Probe(app).HandlerCalls);
    }

    [Fact]
    public async Task Class_constructor_named_values_repeated_declarations_and_actual_generic_objects_are_retained()
    {
        await using var app = Create();
        await app.StartAsync();
        var catalog = Route(app, "/responses/catalog/{id:int}").Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>();
        Assert.IsType<ProducesResponseTypeAttribute>(Assert.Single(catalog, metadata => metadata.StatusCode == 400));
        Assert.IsType<ProducesResponseTypeAttribute<DeclaredFailure>>(Assert.Single(catalog, metadata => metadata.StatusCode == 404));
        var repeated = catalog.Where(metadata => metadata.StatusCode == 418).ToArray();
        Assert.Equal(2, repeated.Length);
        Assert.IsType<ProducesResponseTypeAttribute<DeclaredFailure>>(repeated[0]);
        Assert.IsType<ProducesResponseTypeAttribute>(repeated[1]);
        Assert.Equal(typeof(DeclaredFailure), repeated[0].Type);
        Assert.Equal(typeof(GroupNotice), repeated[1].Type);
        var expected = new ProducesResponseTypeAttribute<DeclaredFailure>(418, "application/json", "application/vnd.catalog-error+json", "application/json");
        var expectedMedia = new MediaTypeCollection();
        var actualMedia = new MediaTypeCollection();
        ((IApiResponseMetadataProvider)expected).SetContentTypes(expectedMedia);
        repeated[0].SetContentTypes(actualMedia);
        Assert.Equal(expectedMedia, actualMedia);
        var overridden = Assert.IsType<ProducesResponseTypeAttribute>(Assert.Single(Route(app, "/responses/defaults").Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>(), metadata => metadata.StatusCode == 422));
        Assert.Equal(typeof(DeclaredFailure), overridden.Type);
    }

    [Fact]
    public async Task Typed_IProduces_metadata_has_native_API_Explorer_priority_over_overlapping_MVC_declarations()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        await app.StartAsync();
        await native.StartAsync();
        var route = Route(app, "/responses/mixed/{id:int}");
        Assert.Contains(route.Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>(), metadata => metadata.StatusCode == 200 && metadata.Type == typeof(DeclaredFailure));
        var success = Assert.Single(Description(app, "/responses/mixed/{id:int}").SupportedResponseTypes, response => response.StatusCode == 200);
        Assert.Equal(typeof(CatalogItem), success.Type);
        Assert.Equal(Assert.Single(Description(native, "/responses/mixed/{id:int}").SupportedResponseTypes, response => response.StatusCode == 200).ApiResponseFormats.Select(format => format.MediaType), success.ApiResponseFormats.Select(format => format.MediaType));
        var migrated = Assert.Single(Description(app, "/responses/migrated/{id:int}").SupportedResponseTypes, response => response.StatusCode == 404);
        Assert.Equal(typeof(MissingItem), migrated.Type);
        Assert.Equal(Assert.Single(Description(native, "/responses/migrated/{id:int}").SupportedResponseTypes, response => response.StatusCode == 404).ApiResponseFormats.Select(format => format.MediaType), migrated.ApiResponseFormats.Select(format => format.MediaType));
    }

    [Fact]
    public async Task An_MVC_only_status_does_not_suppress_the_existing_IProduces_only_fallback()
    {
        await using var app = Create();
        await app.StartAsync();
        var route = Route(app, "/responses/mixed/{id:int}");
        Assert.IsType<ProducesResponseTypeAttribute<DeclaredFailure>>(Assert.Single(route.Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>(), response => response.StatusCode == 404));
        var fallback = Assert.Single(route.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), response => response.StatusCode == 404);
        Assert.Equal(typeof(FallbackNotice), fallback.Type);
        Assert.Equal(new[] { "application/json" }, fallback.ContentTypes);
        Assert.Equal(typeof(FallbackNotice), Assert.Single(Description(app, "/responses/mixed/{id:int}").SupportedResponseTypes, response => response.StatusCode == 404).Type);
    }

    [Fact]
    public async Task Optional_Configure_metadata_precedes_native_filter_declarations_and_preserves_runtime_headers()
    {
        await using var app = Create();
        await app.StartAsync();
        var declarations = Route(app, "/responses/configured/{id:int}").Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>().Where(metadata => metadata.StatusCode == 425).ToArray();
        Assert.Equal(2, declarations.Length);
        Assert.IsType<ProducesResponseTypeAttribute<ConfiguredNotice>>(declarations[0]);
        Assert.IsType<ProducesResponseTypeAttribute<DeclaredFailure>>(declarations[1]);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/responses/configured/7");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("present", Assert.Single(response.Headers.GetValues("X-Native-Configured")));
    }

    [Fact]
    public async Task Namespace_declarations_reach_native_descendants_without_manufacturing_endpoint_identity()
    {
        await using var app = Create();
        await app.StartAsync();
        var native = Route(app, "/responses/native/{id:int}");
        var declarations = native.Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>();
        Assert.IsType<ProducesResponseTypeAttribute>(Assert.Single(declarations, metadata => metadata.StatusCode == 503));
        Assert.IsType<ProducesResponseTypeAttribute<GroupNotice>>(Assert.Single(declarations, metadata => metadata.StatusCode == 429));
        Assert.Null(native.Metadata.GetMetadata<MediumApiEndpointMetadata>());
        Assert.Equal(typeof(GetMethodResponseEndpoint).GetMethod("HandleAsync"), native.Metadata.GetMetadata<MethodInfo>());
    }

    [Fact]
    public async Task Aliases_retain_class_filter_and_method_markers_without_importing_namespace_defaults_or_names()
    {
        await using var app = Create();
        await app.StartAsync();
        foreach (var path in new[] { "/aliases/first/{id:int}", "/aliases/second/{id:int}" })
        {
            var route = Route(app, path);
            var declarations = route.Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>();
            Assert.Equal(new[] { 410, 401, 404 }, declarations.Select(metadata => metadata.StatusCode));
            Assert.DoesNotContain(declarations, metadata => metadata.StatusCode == 503 || metadata.StatusCode == 429);
            Assert.Null(route.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.IEndpointNameMetadata>());
            Assert.Equal("AliasResponseEndpoint", route.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType.Name);
        }
    }

    [Fact]
    public async Task Direct_method_declarations_and_original_method_information_remain_native()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        await app.StartAsync();
        await native.StartAsync();
        var route = Route(app, "/responses/method/{id:int}");
        Assert.Equal(typeof(GetMethodResponseEndpoint).GetMethod("HandleAsync"), route.Metadata.GetMetadata<MethodInfo>());
        Assert.Equal(route.Metadata.GetMetadata<MethodInfo>(), Route(native, "/responses/method/{id:int}").Metadata.GetMetadata<MethodInfo>());
        Assert.Equal(MvcValues(Route(native, "/responses/method/{id:int}")), MvcValues(route));
        Assert.IsType<ProducesResponseTypeAttribute<DeclaredFailure>>(Assert.Single(route.Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>(), metadata => metadata.StatusCode == 404));
        var migratedMethod = Route(app, "/responses/migrated/{id:int}").Metadata.GetMetadata<MethodInfo>()!;
        var legacyMethod = Route(native, "/responses/migrated/{id:int}").Metadata.GetMetadata<MethodInfo>()!;
        Assert.Equal(typeof(Results<Ok<CatalogItem>, NotFound<MissingItem>>), migratedMethod.ReturnType);
        Assert.Equal(legacyMethod.ReturnType, migratedMethod.ReturnType);
        Assert.NotEqual(legacyMethod, migratedMethod);
    }

    [Fact]
    public async Task Explicit_custom_subtype_opt_in_is_forwarded_once_with_its_named_field_at_each_supported_class_level()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<GetCustomResponseEndpoint>("/custom/{id:int}");
        app.MapMediumApiNamespace<CustomResponseGroup>().MapGet("/group-value", () => TypedResults.Ok("native"));
        app.MapGet("/filter", () => TypedResults.Ok("native")).AddMediumApiFilter<CustomResponseFilter>();
        app.MapGroup("/group-filter").AddMediumApiFilter<CustomResponseFilter>().MapGet("/filter-value", () => TypedResults.Ok("native"));
        await app.StartAsync();
        foreach (var item in new (string Path, string Label, int Status)[] { ("/custom/{id:int}", "catalog-contract", 451), ("/custom-group/group-value", "group-field", 452), ("/filter", "filter-field", 454), ("/group-filter/filter-value", "filter-field", 454) })
        {
            var metadata = Assert.Single(Route(app, item.Path).Metadata.GetOrderedMetadata<CatalogResponseAttribute>());
            Assert.Equal(item.Label, metadata.Label);
            Assert.Equal(item.Status, metadata.StatusCode);
        }

        Assert.IsType<ProducesResponseTypeAttribute<DeclaredFailure>>(Assert.Single(Route(app, "/filter").Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>(), metadata => metadata.StatusCode == 455));
    }

    [Fact]
    public async Task Unrelated_derived_and_inherited_class_declarations_are_ignored_without_constructing_them()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<IgnoredResponseEndpoint>();
        app.MapMediumApiNamespace<IgnoredResponseGroup>().MapGet("/ignored-group-value", () => TypedResults.Ok("native"));
        app.MapGet("/filter", () => TypedResults.Ok("native")).AddMediumApiFilter<IgnoredResponseFilter>();
        app.MapGroup("/group-filter").AddMediumApiFilter<IgnoredResponseFilter>().MapGet("/ignored-filter-value", () => TypedResults.Ok("native"));
        await app.StartAsync();
        foreach (var route in Routes(app))
        {
            Assert.Empty(route.Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>());
        }
    }

    [Fact]
    public async Task Independent_hosts_and_repeated_namespace_mounts_receive_fresh_mutable_native_metadata_objects()
    {
        await using var first = Create(mapEndpoints: false);
        await using var second = Create(mapEndpoints: false);
        foreach (var app in new[] { first, second })
        {
            app.MapMediumApiEndpoint<GetCatalogResponseEndpoint>("/catalog/{id:int}");
            foreach (var prefix in new[] { "/one", "/two" })
            {
                app.MapGroup(prefix).MapMediumApiNamespace<CustomResponseGroup>().MapGet("/value", () => TypedResults.Ok("native"));
            }
        }

        await first.StartAsync();
        await second.StartAsync();
        var firstRoute = Route(first, "/catalog/{id:int}");
        var secondRoute = Route(second, "/catalog/{id:int}");
        var firstMetadata = Assert.IsType<ProducesResponseTypeAttribute<DeclaredFailure>>(Assert.Single(firstRoute.Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>(), metadata => metadata.StatusCode == 404));
        var secondMetadata = Assert.IsType<ProducesResponseTypeAttribute<DeclaredFailure>>(Assert.Single(secondRoute.Metadata.GetOrderedMetadata<IApiResponseMetadataProvider>(), metadata => metadata.StatusCode == 404));
        Assert.NotSame(firstMetadata, secondMetadata);
        firstMetadata.StatusCode = 499;
        Assert.Equal(404, secondMetadata.StatusCode);
        var one = Assert.Single(Route(first, "/one/custom-group/value").Metadata.GetOrderedMetadata<CatalogResponseAttribute>());
        var two = Assert.Single(Route(first, "/two/custom-group/value").Metadata.GetOrderedMetadata<CatalogResponseAttribute>());
        Assert.NotSame(one, two);
        one.Label = "changed";
        Assert.Equal("group-field", two.Label);
    }

    [Fact]
    public async Task Disabled_namespace_skips_native_attribute_construction_and_Map()
    {
        await using var app = Create(mapEndpoints: false);
        Assert.Null(app.TryMapMediumApiNamespace<DisabledResponseGroup>());
        await app.StartAsync();
        Assert.Empty(Routes(app));
    }

    [Theory]
    [InlineData("/responses/catalog/7", "catalog")]
    [InlineData("/responses/defaults", "defaults")]
    [InlineData("/responses/method/7", "method")]
    [InlineData("/responses/native/7", "method")]
    [InlineData("/responses/filtered/7", "filtered")]
    [InlineData("/responses/migrated/7", "migrated")]
    [InlineData("/responses/mixed/7", "mixed")]
    [InlineData("/responses/configured/7", "configured")]
    [InlineData("/responses/custom/7", "custom")]
    [InlineData("/aliases/first/7", "alias")]
    [InlineData("/aliases/second/7", "alias")]
    [InlineData("/fluent/7", "fluent")]
    public async Task Metadata_declarations_do_not_rewrite_success_status_payload_or_serialization(string path, string source)
    {
        string? expectedBody = null;
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync();
            if (native)
            {
                expectedBody = body;
            }
            else
            {
                Assert.Equal(expectedBody, body);
            }

            Assert.Equal(new CatalogItem(7, source), await response.Content.ReadFromJsonAsync<CatalogItem>());
            Assert.Equal(1, Probe(app).HandlerCalls);
        }
    }

    [Theory]
    [InlineData("/responses/filtered/0")]
    [InlineData("/responses/migrated/0")]
    [InlineData("/aliases/first/0")]
    [InlineData("/fluent/0")]
    public async Task Expected_missing_results_remain_explicit_native_filter_or_legacy_wrapper_outcomes(string path)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(new MissingItem("missing"), await response.Content.ReadFromJsonAsync<MissingItem>());
            Assert.Equal(0, Probe(app).HandlerCalls);
            Assert.Equal(1, Probe(app).FilterCalls);
        }
    }
}