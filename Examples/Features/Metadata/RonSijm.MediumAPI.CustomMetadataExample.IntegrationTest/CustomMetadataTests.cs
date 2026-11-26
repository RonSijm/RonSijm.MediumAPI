using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.CustomMetadataExample.Endpoints.Api;
using RonSijm.MediumAPI.CustomMetadataExample.Endpoints.Api.Disabled;
using Xunit;
using static RonSijm.MediumAPI.CustomMetadataExample.IntegrationTest.CustomMetadataHost;

namespace RonSijm.MediumAPI.CustomMetadataExample.IntegrationTest;

public sealed class CustomMetadataTests
{
    [Fact]
    public async Task Ordered_custom_values_native_contracts_and_real_API_Explorer_match_without_endpoint_activation()
    {
        await using var app = Create();
        await using var native = Create(native: true);
        await app.StartAsync();
        await native.StartAsync();
        Assert.Equal(12, Routes(app).Count());
        Assert.Equal(Inventory(native), Inventory(app));
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), app.GetMediumApiContractSnapshot()));
        var actual = Descriptions(app);
        var expected = Descriptions(native);
        Assert.Equal(12, actual.Operations.Count);
        Assert.Equal(12, expected.Operations.Count);
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(expected, actual));
        Assert.Equal(0, Probe(app).TargetActivations);
        Assert.Equal(0, Probe(app).Calls);
        Assert.Equal(0, Probe(native).Calls);
    }

    [Fact]
    public async Task Constructor_named_fields_and_properties_retain_enum_array_boxed_enum_and_Type_values()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var value = Assert.Single(Metadata(app, "/metadata/class"), metadata => metadata.Name == "class");
            Assert.Equal(typeof(MetadataReply), value.Contract);
            Assert.Equal(MetadataMode.Second, Assert.IsType<MetadataMode>(value.Boxed));
            Assert.Equal(new[] { MetadataMode.First, MetadataMode.Second }, value.Modes);
            Assert.Equal(new[] { "alpha", "beta" }, value.Aliases);
            Assert.Equal(typeof(string), value.Extra);
            Assert.Null(value.Optional);
            Assert.Equal(MetadataMode.Second, value.Flag);
        }
    }

    [Fact]
    public async Task Null_constructor_and_named_values_remain_null_including_the_params_array()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var value = Assert.Single(Metadata(app, "/metadata/null"), metadata => metadata.Name == "null");
            Assert.Null(value.Contract);
            Assert.Null(value.Boxed);
            Assert.Null(value.Modes);
            Assert.Null(value.Aliases);
            Assert.Null(value.Extra);
            Assert.Null(value.Optional);
            Assert.Equal(MetadataMode.None, value.Flag);
        }
    }

    [Fact]
    public async Task Namespace_class_inference_original_method_Configure_and_final_positions_keep_native_order()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            Assert.Equal(new[] { "map", "group", "class", "provider", "method", "endpoint-configure", "endpoint-final" }, Names(app, "/metadata/order"));
        }
    }

    [Fact]
    public async Task Class_namespace_and_filter_base_attributes_are_ignored_while_the_original_inherited_method_remains()
    {
        await using var app = Create();
        var route = Route(app, "/metadata/inherited");
        Assert.Equal(new[] { "map", "group", "derived-class", "inherited-method", "derived-filter" }, route.Metadata.GetOrderedMetadata<AppMetadataAttribute>().Select(value => value.Name));
        Assert.Equal(typeof(MetadataEndpointBase), route.Metadata.GetMetadata<MethodInfo>()?.DeclaringType);
        Assert.Equal(typeof(InheritedMetadataEndpoint), route.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType);
        Assert.DoesNotContain(route.Metadata.GetOrderedMetadata<AppMetadataAttribute>(), value => value.Name.StartsWith("ignored-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_class_attribute_already_supported_as_native_authorization_is_appended_once_when_it_also_opts_in()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var route = Route(app, "/metadata/dual");
            var access = Assert.Single(route.Metadata.GetOrderedMetadata<IAuthorizeData>());
            Assert.IsType<SampleAccessAttribute>(access);
            Assert.Equal("SampleReaders", access.Policy);
            Assert.Single(route.Metadata.GetOrderedMetadata<IAllowAnonymous>());
        }
    }

    [Fact]
    public async Task An_opted_in_filter_authorization_attribute_is_metadata_once_without_registering_a_second_filter()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var route = Route(app, "/metadata/filter");
            Assert.IsType<SampleAccessAttribute>(Assert.Single(route.Metadata.GetOrderedMetadata<IAuthorizeData>()));
            Assert.Single(route.Metadata.GetOrderedMetadata<IAllowAnonymous>());
            Assert.Equal(new[] { "map", "group", "filter-configure", "filter" }, Names(app, "/metadata/filter"));
        }
    }

    [Fact]
    public async Task Filter_custom_response_metadata_precedes_fallbacks_and_keeps_its_existing_schema_and_media()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var response = Assert.Single(Route(app, "/metadata/filter").Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), metadata => metadata.StatusCode == 404);
            Assert.IsType<CustomResponseAttribute>(response);
            Assert.Equal(typeof(MetadataReply), response.Type);
            Assert.Equal(new[] { "application/x-custom-error" }, response.ContentTypes);
        }
    }

    [Fact]
    public async Task Repeated_actual_declarations_remain_two_distinct_values_in_source_order()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            Assert.Equal(new[] { "map", "group", "first", "second" }, Names(app, "/metadata/repeated"));
            var values = Metadata(app, "/metadata/repeated").Where(value => value.Name is "first" or "second").ToArray();
            Assert.Equal(2, values.Length);
            Assert.NotSame(values[0], values[1]);
        }
    }

    [Fact]
    public async Task Disabled_definitions_skip_opted_in_throwing_constructors_and_do_not_expose_routes()
    {
        await using var app = Create();
        Assert.Null(app.TryMapMediumApiNamespace<DisabledMetadataDefinition>());
        Assert.DoesNotContain(Routes(app), route => route.RoutePattern.RawText?.Contains("disabled", StringComparison.Ordinal) == true);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/metadata/disabled");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, Probe(app).Calls);
    }

    [Fact]
    public async Task Explicit_aliases_keep_their_own_markers_without_namespace_metadata_or_automatic_declared_patterns()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            Assert.Equal(new[] { "manual" }, Names(app, "/aliases/first"));
            Assert.Equal(new[] { "manual" }, Names(app, "/aliases/second"));
            Assert.Equal("FirstMetadataAlias", Route(app, "/aliases/first").Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
            Assert.Equal("SecondMetadataAlias", Route(app, "/aliases/second").Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync("/unmapped");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task An_attribute_implementing_a_static_metadata_provider_is_stored_without_invoking_its_provider_method()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            Assert.Single(Route(app, "/metadata/static").Metadata.GetOrderedMetadata<StaticMetadataAttribute>());
            Assert.Equal(new[] { "map", "group" }, Names(app, "/metadata/static"));
        }
    }

    [Fact]
    public async Task Independent_hosts_receive_distinct_attribute_instances_and_constructor_and_named_arrays()
    {
        await using var first = Create();
        await using var second = Create();
        var left = Assert.Single(Metadata(first, "/metadata/class"), value => value.Name == "class");
        var right = Assert.Single(Metadata(second, "/metadata/class"), value => value.Name == "class");
        Assert.NotSame(left, right);
        Assert.NotSame(left.Modes, right.Modes);
        Assert.NotSame(left.Aliases, right.Aliases);
        left.Modes![0] = MetadataMode.None;
        left.Aliases![0] = "changed";
        Assert.Equal(MetadataMode.First, right.Modes![0]);
        Assert.Equal("alpha", right.Aliases![0]);
        Assert.Equal(0, Probe(first).TargetActivations);
        Assert.Equal(0, Probe(second).TargetActivations);
    }

    [Fact]
    public async Task Repeated_explicit_namespace_mounts_create_independent_metadata_at_their_native_prefixes()
    {
        await using var app = Create(mapEndpoints: false);
        foreach (var prefix in new[] { "first", "second" })
        {
            var mount = app.MapGroup("/" + prefix).WithoutDefaultEndpointName().MapMediumApiNamespace<MetadataGroupDefinition>();
            // Preserve explicit native names while making repeated mounts valid in the same host.
            ((IEndpointConventionBuilder)mount).Finally(builder =>
            {
                var name = builder.Metadata.OfType<IEndpointNameMetadata>().LastOrDefault()?.EndpointName;
                if (name is not null)
                {
                    builder.Metadata.Add(new EndpointNameMetadata(prefix + "." + name));
                    builder.Metadata.Add(new RouteNameMetadata(prefix + "." + name));
                }
            });
        }

        var first = Assert.Single(Metadata(app, "/first/metadata/class"), value => value.Name == "class");
        var second = Assert.Single(Metadata(app, "/second/metadata/class"), value => value.Name == "class");
        Assert.NotSame(first, second);
        Assert.NotSame(first.Modes, second.Modes);
        Assert.NotSame(first.Aliases, second.Aliases);
        Assert.Equal(20, Routes(app).Count());
        Assert.Equal(0, Probe(app).TargetActivations);
    }

    [Fact]
    public async Task Original_handler_attributes_without_the_opt_in_interface_keep_their_existing_native_path_once()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            var value = Assert.Single(Route(app, "/metadata/method").Metadata.GetOrderedMetadata<UnmarkedMethodMetadataAttribute>());
            Assert.Equal("native-method", value.Name);
            Assert.False(typeof(IEndpointMetadataAttribute).IsAssignableFrom(value.GetType()));
        }
    }

    [Fact]
    public async Task Unmarked_throwing_class_attributes_are_not_constructed_or_forwarded_by_any_new_reader()
    {
        foreach (var owner in new[] { typeof(MetadataGroupDefinition), typeof(ClassMetadataEndpoint), typeof(ConfiguredMetadataFilter), typeof(DerivedMetadataFilter), typeof(InheritedMetadataEndpoint) })
        {
            Assert.Contains(owner.GetCustomAttributesData(), declaration => declaration.AttributeType == typeof(UnmarkedThrowingAttribute));
        }

        await using var app = Create();
        Assert.All(Routes(app), route => Assert.Empty(route.Metadata.GetOrderedMetadata<UnmarkedThrowingAttribute>()));
        Assert.Equal(0, Probe(app).TargetActivations);
    }

    [Theory]
    [InlineData("/metadata/class", "class", "map,group,class")]
    [InlineData("/metadata/method", "method", "map,group,method")]
    [InlineData("/metadata/filter", "filter", "map,group,filter-configure,filter")]
    [InlineData("/metadata/order", "order", "map,group,class,provider,method,endpoint-configure,endpoint-final")]
    [InlineData("/metadata/inherited", "inherited", "map,group,derived-class,inherited-method,derived-filter")]
    [InlineData("/metadata/repeated", "repeated", "map,group,first,second")]
    [InlineData("/metadata/null", "null", "map,group,null")]
    [InlineData("/metadata/static", "static", "map,group")]
    [InlineData("/metadata/dual", "dual", "map,group")]
    [InlineData("/metadata/native", "native", "map,group")]
    [InlineData("/aliases/first", "alias", "manual")]
    [InlineData("/aliases/second", "alias", "manual")]
    public async Task Native_HTTP_results_and_application_metadata_reading_preserve_status_payload_headers_and_names(string path, string source, string expectedLabels)
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

            var reply = (await response.Content.ReadFromJsonAsync<MetadataReply>())!;
            Assert.Equal(source, reply.Source);
            Assert.Equal(expectedLabels.Split(','), reply.Labels);
            if (path.StartsWith("/aliases/", StringComparison.Ordinal))
            {
                Assert.False(response.Headers.Contains("X-Sample-Metadata"));
            }
            else
            {
                Assert.Equal(expectedLabels, Assert.Single(response.Headers.GetValues("X-Sample-Metadata")));
            }

            Assert.Equal(1, Probe(app).Calls);
        }
    }
}