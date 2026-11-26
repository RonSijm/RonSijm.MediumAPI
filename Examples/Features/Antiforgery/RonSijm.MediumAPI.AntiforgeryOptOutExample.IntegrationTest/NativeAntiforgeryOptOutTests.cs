using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using RonSijm.MediumAPI.AntiforgeryOptOutExample.Endpoints;
using RonSijm.MediumAPI.AntiforgeryOptOutExample.Endpoints.Unavailable;
using RonSijm.MediumAPI.AntiforgeryOptOutExample.IntegrationTest.ScopeFixtures;
using Xunit;
using static RonSijm.MediumAPI.AntiforgeryOptOutExample.IntegrationTest.OptOutHost;

namespace RonSijm.MediumAPI.AntiforgeryOptOutExample.IntegrationTest;

public sealed class NativeAntiforgeryOptOutTests
{
    [Fact]
    public async Task Opt_outs_match_independent_native_routes_API_descriptions_and_validation_metadata_order()
    {
        await using var generated = Create();
        await using var native = Create(native: true);
        await generated.StartAsync();
        await native.StartAsync();

        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(DescriptionSnapshot(native), DescriptionSnapshot(generated)));
        Assert.Equal(ValidationInventory(native), ValidationInventory(generated));
        Assert.Equal(9, Routes(generated).Length);
        Assert.Equal(9, Descriptions(generated).Length);
        Assert.Equal(0, Probe(generated).HandlerCalls);
        Assert.Equal(0, Probe(generated).FilterCalls);
    }

    [Theory]
    [InlineData("/opt-out/required", true)]
    [InlineData("/opt-out/class", false)]
    [InlineData("/opt-out/method", false)]
    [InlineData("/opt-out/filter", false)]
    [InlineData("/opt-out/group/form", false)]
    [InlineData("/opt-out/filter-group/form", false)]
    [InlineData("/aliases/class-form", false)]
    [InlineData("/aliases/required-again", true)]
    public async Task Native_effective_route_and_description_metadata_retain_the_final_convention_priorities(string path, bool requiresValidation)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            Assert.Equal(requiresValidation, Route(app, path).Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
            Assert.Equal(requiresValidation, DescriptionMetadata(app, path).GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        }
    }

    [Theory]
    [InlineData("/opt-out/class", 0)]
    [InlineData("/opt-out/method", 0)]
    [InlineData("/opt-out/filter", 1)]
    [InlineData("/opt-out/group/form", 0)]
    [InlineData("/opt-out/filter-group/form", 1)]
    [InlineData("/aliases/class-form", 0)]
    public async Task Final_opt_outs_skip_native_validation_and_allow_bound_forms_even_with_invalid_tokens(string path, int filterCalls)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await Post(client, path, SampleAntiforgery.CookieName + "=synthetic-invalid-cookie", "synthetic-invalid-token");

            await AssertReceipt(response);
            Assert.Equal(1, Probe(app).HandlerCalls);
            Assert.Equal(filterCalls, Probe(app).FilterCalls);
        }
    }

    [Theory]
    [InlineData("/opt-out/required", false)]
    [InlineData("/opt-out/required", true)]
    [InlineData("/aliases/required-again", false)]
    [InlineData("/aliases/required-again", true)]
    public async Task Required_routes_and_later_host_final_requirements_still_accept_valid_cookie_header_or_form_tokens(string path, bool formToken)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var issued = await Issue(client);
            using var response = await Post(client, path, issued.Cookie, formToken ? null : issued.Tokens.RequestToken, formToken ? issued.Tokens.RequestToken : null);

            await AssertReceipt(response, validationSucceeded: true);
            Assert.Equal(1, Probe(app).HandlerCalls);
        }
    }

    [Theory]
    [InlineData("/opt-out/required", false)]
    [InlineData("/opt-out/required", true)]
    [InlineData("/aliases/required-again", false)]
    [InlineData("/aliases/required-again", true)]
    public async Task Required_routes_and_later_host_final_requirements_reject_missing_or_invalid_tokens_before_handler_work(string path, bool invalidToken)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var issued = await Issue(client);
            using var response = await Post(client, path, issued.Cookie, invalidToken ? "synthetic-invalid-token" : null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, Probe(app).HandlerCalls);
            Assert.Equal(0, Probe(app).FilterCalls);
        }
    }

    [Fact]
    public async Task Disabling_antiforgery_retains_native_form_content_type_rejection()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.PostAsJsonAsync(Root + "/class", new { message = "synthetic-form-value" });

            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            Assert.Equal(0, Probe(app).HandlerCalls);
        }
    }

    [Fact]
    public async Task Disabled_namespace_groups_skip_mapping_and_do_not_publish_descendants()
    {
        await using var app = Create();
        Assert.Null(app.TryMapMediumApiNamespace<UnavailableGroupDefinition>());
        Assert.DoesNotContain(Routes(app), endpoint => endpoint.RoutePattern.RawText?.Contains("Unavailable", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Equal(9, Routes(app).Length);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await Post(client, Root + "/unavailable/form");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, Probe(app).HandlerCalls);
    }

    [Fact]
    public async Task Antiforgery_conventions_do_not_synthesize_error_response_descriptions()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            Assert.All(Routes(app), endpoint => Assert.DoesNotContain(endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(), value => value.StatusCode == StatusCodes.Status400BadRequest));
            Assert.All(Descriptions(app), description => Assert.DoesNotContain(description.SupportedResponseTypes, value => value.StatusCode == StatusCodes.Status400BadRequest));
        }
    }

    [Fact]
    public void Attribute_usage_is_sealed_single_class_or_method_and_retains_native_method_inheritance()
    {
        var marker = typeof(DisableEndpointAntiforgeryAttribute);
        var usage = marker.GetCustomAttribute<AttributeUsageAttribute>()!;

        Assert.True(marker.IsSealed);
        Assert.Equal(AttributeTargets.Class | AttributeTargets.Method, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.True(usage.Inherited);
    }

    [Fact]
    public async Task Aliases_retain_the_original_endpoint_identity_and_only_suppress_the_default_name()
    {
        await using var app = Create();
        var original = Route(app, Root + "/class");
        var alias = Route(app, "/aliases/class-form");
        var requiredAgain = Route(app, "/aliases/required-again");

        Assert.Equal("PostClassOptOutForm", original.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Null(alias.Metadata.GetMetadata<IEndpointNameMetadata>());
        Assert.Null(requiredAgain.Metadata.GetMetadata<IEndpointNameMetadata>());
        Assert.All(new[] { original, alias, requiredAgain }, endpoint =>
        {
            Assert.Equal(typeof(PostClassOptOutFormEndpoint), endpoint.Metadata.GetMetadata<MediumApiEndpointMetadata>()!.EndpointType);
            Assert.Equal(typeof(PostClassOptOutFormEndpoint), endpoint.Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        });
    }

    [Fact]
    public async Task Original_MethodInfo_and_native_bound_arguments_remain_available_to_later_filters()
    {
        await using var app = Create(mapEndpoints: false);
        var invoked = false;
        app.MapMediumApiEndpoint<PostMethodOptOutFormEndpoint>("/fixtures/bound-arguments").AddEndpointFilterFactory((factory, next) =>
        {
            Assert.Equal(typeof(PostMethodOptOutFormEndpoint), factory.MethodInfo.DeclaringType);
            return async context =>
            {
                Assert.Equal("synthetic-form-value", context.GetArgument<string>(0));
                Assert.Same(context.HttpContext, context.GetArgument<HttpContext>(1));
                Assert.Same(Probe(app), context.GetArgument<FormProbe>(2));
                invoked = true;
                return await next(context);
            };
        });
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await Post(client, "/fixtures/bound-arguments");

        await AssertReceipt(response);
        Assert.True(invoked);
    }

    [Fact]
    public async Task A_CLR_base_class_marker_does_not_disable_an_unmarked_concrete_endpoint()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<InheritedClassMarkerEndpoint>();
        Assert.True(Route(app, "/fixtures/class-inheritance").Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await Post(client, "/fixtures/class-inheritance");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, Probe(app).HandlerCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Inherited_and_overridden_handlers_keep_inherited_method_opt_outs(bool overridden)
    {
        await using var app = Create(mapEndpoints: false);
        if (overridden)
        {
            app.MapMediumApiEndpoint<OverrideMethodMarkerEndpoint>();
        }
        else
        {
            app.MapMediumApiEndpoint<InheritedMethodMarkerEndpoint>();
        }

        var path = overridden ? "/fixtures/method-override" : "/fixtures/method-inheritance";
        var endpoint = Route(app, path);
        Assert.False(endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        Assert.Equal(overridden ? typeof(OverrideMethodMarkerEndpoint) : typeof(MarkedMethodBase), endpoint.Metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await Post(client, path);

        await AssertReceipt(response);
        Assert.Equal(1, Probe(app).HandlerCalls);
    }

    [Fact]
    public async Task A_CLR_base_filter_marker_is_not_imported_by_the_MediumAPI_filter_registration()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<PlainFormEndpoint>().AddMediumApiFilter<InheritedFilterMarker>();
        Assert.True(Route(app, "/fixtures/plain").Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await Post(client, "/fixtures/plain");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, Probe(app).HandlerCalls);
    }

    [Fact]
    public async Task Native_AddEndpointFilter_keeps_its_original_behavior_without_importing_MediumAPI_conventions()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<PlainFormEndpoint>().AddEndpointFilter<OptOutFilter>();
        Assert.True(Route(app, "/fixtures/plain").Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await Post(client, "/fixtures/plain");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, Probe(app).HandlerCalls);
        Assert.Equal(0, Probe(app).FilterCalls);
    }

    [Fact]
    public async Task Only_the_direct_marker_is_constructed_while_unrelated_class_attributes_remain_untouched()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<UnrelatedAttributeEndpoint>();
        Assert.False(Route(app, "/fixtures/unrelated").Metadata.GetMetadata<IAntiforgeryMetadata>()!.RequiresValidation);
        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await Post(client, "/fixtures/unrelated");

        await AssertReceipt(response);
        Assert.Equal(1, Probe(app).HandlerCalls);
    }
}