using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.DocumentGroupsExample.Endpoints.Catalog;
using RonSijm.MediumAPI.DocumentGroupsExample.Endpoints.Catalog.Admin;
using RonSijm.MediumAPI.DocumentGroupsExample.Endpoints.Catalog.Child;
using RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest.Fixtures.Disabled;
using RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest.Fixtures.Disabled.Child;
using RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest.Fixtures.Ignored;
using RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest.Fixtures.Ordered;
using Xunit;
using static RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest.DocumentGroupsHost;
using ExampleProgram = RonSijm.MediumAPI.DocumentGroupsExample.Program;

namespace RonSijm.MediumAPI.DocumentGroupsExample.IntegrationTest;

public sealed class DocumentGroupScopeTests
{
    [Fact]
    public async Task Native_order_preserves_Map_class_callback_method_and_Configure_group_selection()
    {
        await using var native = Create(mapEndpoints: false);
        await using var generated = Create(mapEndpoints: false);
        var group = native.MapGroup("/ordered").WithGroupName("map").WithMetadata(new StageMarker("Map"));
        group.WithMetadata(new EndpointGroupNameAttribute("class"));
        group.WithGroupName("callback").WithMetadata(new StageMarker("Callback"));
        group.MapGet("/value", () => TypedResults.Ok(new FixtureResponse("value"))).WithName("GetOrderedValue");
        group.MapGet("/method", NativeMethod).WithName("GetOrderedMethod");
        group.MapGet("/configured", NativeConfigured).WithGroupName("configured").WithMetadata(new StageMarker("Configure")).WithName("GetOrderedConfigured");
        generated.MapMediumApiNamespace<OrderedGroupDefinition>(builder => builder.WithGroupName("callback").WithMetadata(new StageMarker("Callback")));
        await native.StartAsync();
        await generated.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.Equal(GroupInventory(native), GroupInventory(generated));
        var value = Route(generated, "/ordered/value").Metadata;
        Assert.Equal(new[] { "map", "class", "callback" }, value.GetOrderedMetadata<IEndpointGroupNameMetadata>().Select(metadata => metadata.EndpointGroupName));
        Assert.Equal("callback", value.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
        Assert.Equal("method", Route(generated, "/ordered/method").Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
        var configured = Route(generated, "/ordered/configured").Metadata;
        var names = configured.GetOrderedMetadata<EndpointGroupNameAttribute>();
        Assert.Equal(new[] { "map", "class", "callback", "method", "configured" }, names.Select(metadata => metadata.EndpointGroupName));
        var ordered = configured.ToList();
        Assert.True(ordered.FindIndex(metadata => metadata is StageMarker { Stage: "Map" }) < ordered.IndexOf(names[1]));
        Assert.True(ordered.IndexOf(names[1]) < ordered.FindIndex(metadata => metadata is StageMarker { Stage: "Callback" }));
        Assert.True(ordered.FindIndex(metadata => metadata is StageMarker { Stage: "Callback" }) < ordered.IndexOf(names[3]));
        Assert.True(ordered.IndexOf(names[3]) < ordered.FindIndex(metadata => metadata is StageMarker { Stage: "Configure" }));
    }

    [Fact]
    public async Task A_typed_parent_callback_changes_inherited_document_selection_without_rewriting_routes_or_method_overrides()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            if (native)
            {
                MapNativeCatalog(app, group => group.WithGroupName("callback"));
            }
            else
            {
                app.MapMediumApiNamespace<CatalogGroupDefinition>(group => group.WithGroupName("callback"));
            }

            await app.StartAsync();
            Assert.Equal(5, Routes(app).Count());
            Assert.Equal("callback", Route(app, "/catalog/value").Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
            Assert.Equal("callback", Route(app, "/catalog/child/value").Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
            Assert.Equal(DocumentGroups.Admin, Route(app, "/catalog/detail").Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
            Assert.Equal(DocumentGroups.Admin, Route(app, "/catalog/admin/value").Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
            using var client = app.GetTestClient();
            Assert.Equal(new DocumentResponse("catalog"), await Read(client, "/catalog/value"));
            Assert.Equal(new DocumentResponse("admin"), await Read(client, "/catalog/admin/value"));
            Assert.Equal(new DocumentResponse("child"), await Read(client, "/catalog/child/value"));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Direct_child_mounts_keep_only_the_selected_prefix_and_its_own_document_group(bool admin)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            var prefix = admin ? "/admin" : "/child";
            var kind = admin ? "admin" : "child";
            if (native)
            {
                var group = app.MapGroup(prefix);
                if (admin)
                {
                    group.WithGroupName(DocumentGroups.Admin);
                }

                group.MapGet("/value", (DocumentProbe probe) => TypedResults.Ok(probe.Record(kind)));
            }
            else if (admin)
            {
                app.MapMediumApiNamespace<AdminGroupDefinition>();
            }
            else
            {
                app.MapMediumApiNamespace<ChildGroupDefinition>();
            }

            await app.StartAsync();
            var route = Assert.Single(Routes(app));
            Assert.Equal(prefix + "/value", route.RoutePattern.RawText);
            Assert.Equal(admin ? DocumentGroups.Admin : null, route.Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName);
            Assert.Equal(admin ? DocumentGroups.Admin : null, Assert.Single(Descriptions(app).Operations).GroupName);
            using var client = app.GetTestClient();
            Assert.Equal(new DocumentResponse(kind), await Read(client, prefix + "/value"));
            using var absentAncestor = await client.GetAsync("/catalog" + prefix + "/value");
            Assert.Equal(HttpStatusCode.NotFound, absentAncestor.StatusCode);
        }
    }

    [Fact]
    public async Task Independent_hosts_receive_fresh_native_attributes_and_independent_description_groups()
    {
        await using var first = Create(mapEndpoints: false);
        await using var second = Create(mapEndpoints: false);
        first.MapMediumApiNamespace<CatalogGroupDefinition>();
        second.MapMediumApiNamespace<CatalogGroupDefinition>(group => group.WithGroupName(DocumentGroups.Admin));
        await first.StartAsync();
        await second.StartAsync();
        var firstDeclared = Assert.Single(Route(first, "/catalog/value").Metadata.GetOrderedMetadata<EndpointGroupNameAttribute>(), metadata => metadata.EndpointGroupName == DocumentGroups.Public);
        var secondDeclared = Assert.Single(Route(second, "/catalog/value").Metadata.GetOrderedMetadata<EndpointGroupNameAttribute>(), metadata => metadata.EndpointGroupName == DocumentGroups.Public);
        Assert.NotSame(firstDeclared, secondDeclared);
        Assert.Equal(DocumentGroups.Public, Route(first, "/catalog/value").Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
        Assert.Equal(DocumentGroups.Admin, Route(second, "/catalog/value").Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
        Assert.Equal(DocumentGroups.Public, Assert.Single(Descriptions(first).Operations, operation => operation.Name == "GetCatalogValue").GroupName);
        Assert.Equal(DocumentGroups.Admin, Assert.Single(Descriptions(second).Operations, operation => operation.Name == "GetCatalogValue").GroupName);
    }

    [Fact]
    public async Task Repeated_mounts_have_distinct_metadata_and_names_can_be_suppressed_without_merging_document_groups()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapGroup("/first").MapMediumApiNamespace<CatalogGroupDefinition>(group => group.WithoutDefaultEndpointName());
        app.MapGroup("/second").MapMediumApiNamespace<CatalogGroupDefinition>(group => group.WithGroupName(DocumentGroups.Admin).WithoutDefaultEndpointName());
        await app.StartAsync();
        var first = Route(app, "/first/catalog/value");
        var second = Route(app, "/second/catalog/value");
        Assert.NotSame(Assert.Single(first.Metadata.GetOrderedMetadata<EndpointGroupNameAttribute>()), Assert.Single(second.Metadata.GetOrderedMetadata<EndpointGroupNameAttribute>(), metadata => metadata.EndpointGroupName == DocumentGroups.Public));
        Assert.Equal(DocumentGroups.Public, first.Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
        Assert.Equal(DocumentGroups.Admin, second.Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
        Assert.All(Routes(app), route => Assert.Null(route.Metadata.GetMetadata<IEndpointNameMetadata>()));
        Assert.Equal(10, Routes(app).Count());
        using var client = app.GetTestClient();
        Assert.Equal(new DocumentResponse("catalog"), await Read(client, "/first/catalog/value"));
        Assert.Equal(new DocumentResponse("catalog"), await Read(client, "/second/catalog/value"));
        var publicPaths = Paths(OpenApi(app, DocumentGroups.Public));
        Assert.Equal(new[] { "/first/catalog/child/value", "/first/catalog/value" }, publicPaths);
        Assert.Equal(6, Paths(OpenApi(app, DocumentGroups.Admin)).Length);
    }

    [Fact]
    public async Task Base_and_unrelated_interface_declarations_remain_ignored_while_endpoint_class_metadata_applies()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<IgnoredGroupDefinition>();
        await app.StartAsync();
        var metadata = Route(app, "/ignored/value").Metadata;
        Assert.Equal("endpoint-class", Assert.Single(metadata.GetOrderedMetadata<IEndpointGroupNameMetadata>()).EndpointGroupName);
        Assert.DoesNotContain(metadata, value => value is UnrelatedGroupNameAttribute);
        Assert.Equal("endpoint-class", Assert.Single(Descriptions(app).Operations).GroupName);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/ignored/value");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new FixtureResponse("ignored"), await response.Content.ReadFromJsonAsync<FixtureResponse>());
    }

    [Fact]
    public async Task Disabled_parents_skip_mapping_metadata_construction_descendant_conditions_and_callbacks()
    {
        await using var app = Create(mapEndpoints: false);
        var callbackInvoked = false;
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>(_ => callbackInvoked = true));
        Assert.Null(app.TryMapMediumApiNamespace<DisabledChildGroupDefinition>(_ => callbackInvoked = true));
        Assert.Throws<InvalidOperationException>(() => app.MapMediumApiNamespace<DisabledGroupDefinition>());
        Assert.False(callbackInvoked);
        await app.StartAsync();
        Assert.Empty(Routes(app));
        Assert.Empty(Descriptions(app).Operations);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/disabled-parent");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Group_metadata_does_not_register_API_Explorer_or_a_document_provider()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(DocumentGroupScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();
        app.MapMediumApiNamespace<OrderedGroupDefinition>();
        await app.StartAsync();
        Assert.Null(app.Services.GetService<IApiDescriptionGroupCollectionProvider>());
        Assert.Null(app.Services.GetService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>());
        Assert.Equal("class", Route(app, "/ordered/value").Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/ordered/value");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new FixtureResponse("value"), await response.Content.ReadFromJsonAsync<FixtureResponse>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Typed_callback_group_changes_affect_only_the_provider_selection_of_the_composed_subtree(bool admin)
    {
        var selectedGroup = admin ? DocumentGroups.Admin : DocumentGroups.Public;
        await using var native = Create(mapEndpoints: false);
        await using var generated = Create(mapEndpoints: false);
        MapNativeCatalog(native, group => group.WithGroupName(selectedGroup));
        generated.MapMediumApiNamespace<CatalogGroupDefinition>(group => group.WithGroupName(selectedGroup));
        await native.StartAsync();
        await generated.StartAsync();
        foreach (var documentName in new[] { DocumentGroups.Public, DocumentGroups.Admin })
        {
            Equivalent(MediumApiContractComparison.CompareOpenApi(ExampleProgram.GetOpenApiJson(native.Services, documentName), ExampleProgram.GetOpenApiJson(generated.Services, documentName)));
        }

        var publicPaths = Paths(OpenApi(generated, DocumentGroups.Public));
        var adminPaths = Paths(OpenApi(generated, DocumentGroups.Admin));
        Assert.Equal(admin ? System.Array.Empty<string>() : new[] { "/catalog/child/value", "/catalog/value" }, publicPaths);
        Assert.Equal(admin ? new[] { "/catalog/admin/value", "/catalog/child/value", "/catalog/detail", "/catalog/value" } : new[] { "/catalog/admin/value", "/catalog/detail" }, adminPaths);
        Assert.Equal(5, Routes(generated).Count());
    }

    [EndpointGroupName("method")]
    private static Ok<FixtureResponse> NativeMethod() => TypedResults.Ok(new FixtureResponse("method"));

    [EndpointGroupName("method")]
    private static Ok<FixtureResponse> NativeConfigured() => TypedResults.Ok(new FixtureResponse("configured"));
}