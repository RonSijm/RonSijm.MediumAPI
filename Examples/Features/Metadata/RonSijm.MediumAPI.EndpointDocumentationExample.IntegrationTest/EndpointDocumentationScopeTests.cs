using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Composed.Child;
using RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Disabled;
using RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Disabled.Child;
using RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Inheritance;
using RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Manual;
using RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Ordered;
using RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Scoped;
using RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.ScopeFixtures.Selected;
using Xunit;
using static RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest.EndpointDocumentationHost;
using ExampleProgram = RonSijm.MediumAPI.EndpointDocumentationExample.Program;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.IntegrationTest;

public sealed class EndpointDocumentationScopeTests
{
    [Fact]
    public async Task Class_metadata_precedes_native_providers_methods_Configure_and_endpoint_then_group_final_conventions()
    {
        await using var native = Create(mapEndpoints: false);
        await using var generated = Create(mapEndpoints: false);
        var nativeGroup = native.MapGroup("/scope/ordered").WithTags("scope-group").WithGroupName("scope-group").WithMetadata(new VisibilityMetadata(true));
        ((IEndpointConventionBuilder)nativeGroup).Finally(builder =>
        {
            builder.Metadata.Add(new TagsAttribute("scope-group-final"));
            builder.Metadata.Add(new EndpointGroupNameAttribute(DocumentationGroups.Public));
            builder.Metadata.Add(new VisibilityMetadata(false));
        });
        var classGroup = nativeGroup.MapGroup("");
        classGroup.WithMetadata(new TagsAttribute("scope-class", "scope-second"), new EndpointGroupNameAttribute("scope-class"), new ExcludeFromDescriptionAttribute());
        var nativeEndpoint = classGroup.MapGet("/value", NativeOrdered).WithName("Ordered");
        nativeEndpoint.WithTags("scope-configured").WithGroupName("scope-configured").WithMetadata(new VisibilityMetadata(false));
        nativeEndpoint.Finally(builder =>
        {
            builder.Metadata.Add(new TagsAttribute("scope-endpoint-final"));
            builder.Metadata.Add(new EndpointGroupNameAttribute("scope-endpoint-final"));
            builder.Metadata.Add(new VisibilityMetadata(false));
        });
        generated.MapMediumApiNamespace<OrderedGroupDefinition>().MapMediumApiEndpoint<OrderedEndpoint>();
        var metadata = Route(generated, "/scope/ordered/value").Metadata;
        Assert.Equal(new[] { "scope-group", "scope-class,scope-second", "scope-provider", "scope-method", "scope-configured", "scope-endpoint-final", "scope-group-final" }, metadata.GetOrderedMetadata<ITagsMetadata>().Select(value => string.Join(",", value.Tags)));
        Assert.Equal(new[] { "scope-group", "scope-class", "scope-provider", "scope-method", "scope-configured", "scope-endpoint-final", DocumentationGroups.Public }, metadata.GetOrderedMetadata<IEndpointGroupNameMetadata>().Select(value => value.EndpointGroupName));
        Assert.Equal(new[] { true, true, false, true, false, false, false }, metadata.GetOrderedMetadata<IExcludeFromDescriptionMetadata>().Select(value => value.ExcludeFromDescription));
        Assert.Equal(typeof(OrderedEndpoint), metadata.GetMetadata<MethodInfo>()!.DeclaringType);
        Assert.Equal(MetadataInventory(native), MetadataInventory(generated));
        await native.StartAsync();
        await generated.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.Equal(DocumentationGroups.Public, Assert.Single(ApiDescriptions(generated)).GroupName);
    }

    [Fact]
    public async Task CLR_base_class_declarations_are_ignored_while_original_inherited_method_declarations_remain()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<BaseClassIgnoredEndpoint>();
        app.MapMediumApiEndpoint<InheritedMethodEndpoint>();
        var plain = Route(app, "/scope/base-class-ignored").Metadata;
        Assert.Null(plain.GetMetadata<ITagsMetadata>());
        Assert.Null(plain.GetMetadata<IEndpointGroupNameMetadata>());
        Assert.Null(plain.GetMetadata<IExcludeFromDescriptionMetadata>());
        var inherited = Route(app, "/scope/inherited-method").Metadata;
        Assert.Equal(new[] { DocumentationTags.Catalog, DocumentationTags.Method }, inherited.GetOrderedMetadata<ITagsMetadata>().Select(value => Assert.Single(value.Tags)));
        Assert.Equal(new[] { DocumentationGroups.Public, DocumentationGroups.Admin }, inherited.GetOrderedMetadata<IEndpointGroupNameMetadata>().Select(value => value.EndpointGroupName));
        Assert.True(inherited.GetMetadata<IExcludeFromDescriptionMetadata>()!.ExcludeFromDescription);
        Assert.Equal(typeof(BaseMethodEndpoint), inherited.GetMetadata<MethodInfo>()!.DeclaringType);
        await app.StartAsync();
        Assert.Equal("scope/base-class-ignored", Assert.Single(ApiDescriptions(app)).RelativePath);
        using var client = app.GetTestClient();
        Assert.Equal(new DocumentationResponse("base-class-ignored"), await Read(client, "/scope/base-class-ignored"));
        Assert.Equal(new DocumentationResponse("inherited-method"), await Read(client, "/scope/inherited-method"));
    }

    [Fact]
    public async Task Class_metadata_ignores_unselected_attributes_while_provider_reflection_keeps_native_failures()
    {
        Assert.True(typeof(TagsAttribute).IsSealed);
        Assert.True(typeof(EndpointGroupNameAttribute).IsSealed);
        Assert.True(typeof(ExcludeFromDescriptionAttribute).IsSealed);
        var errors = new List<Exception>();
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            if (native)
            {
                var target = new IgnoredAttributesEndpoint();
                app.MapGet("/scope/ignored-attributes", target.HandleAsync).WithMetadata(new TagsAttribute(DocumentationTags.Catalog), new EndpointGroupNameAttribute(DocumentationGroups.Public)).WithName("IgnoredAttributes");
            }
            else
            {
                app.MapMediumApiEndpoint<IgnoredAttributesEndpoint>();
            }

            var metadata = Route(app, "/scope/ignored-attributes").Metadata;
            Assert.Equal(DocumentationTags.Catalog, Assert.Single(metadata.GetMetadata<ITagsMetadata>()!.Tags));
            Assert.Equal(DocumentationGroups.Public, metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
            Assert.Null(metadata.GetMetadata<IExcludeFromDescriptionMetadata>());
            Assert.DoesNotContain(metadata, value => value is UnrelatedTagsAttribute or UnrelatedGroupNameAttribute or UnrelatedExclusionAttribute);
            await app.StartAsync();
            Assert.Equal(DocumentationGroups.Public, Assert.Single(ApiDescriptions(app)).GroupName);
            Assert.Equal(0, Probe(app).Calls);
            var error = Record.Exception(() => OpenApi(app, DocumentationGroups.Public));
            Assert.NotNull(error);
            errors.Add(error.GetBaseException());
            Assert.Equal(0, Probe(app).Calls);
            using var client = app.GetTestClient();
            Assert.Equal(new DocumentationResponse("ignored-attributes"), await Read(client, "/scope/ignored-attributes"));
        }

        Assert.IsType<InvalidOperationException>(errors[0]);
        Assert.Equal(errors[0].GetType(), errors[1].GetType());
        Assert.Equal(errors[0].Message, errors[1].Message);
    }

    [Fact]
    public async Task Repeated_mounts_and_hosts_receive_fresh_attributes_and_tag_collections_with_mutation_isolation()
    {
        await using var repeated = Create(mapEndpoints: false);
        await using var independent = Create(mapEndpoints: false);
        repeated.MapMediumApiEndpoint<FreshMetadataEndpoint>("/scope/first").WithName("FirstFresh");
        repeated.MapMediumApiEndpoint<FreshMetadataEndpoint>("/scope/second").WithName("SecondFresh");
        independent.MapMediumApiEndpoint<FreshMetadataEndpoint>();
        var first = Route(repeated, "/scope/first").Metadata;
        var second = Route(repeated, "/scope/second").Metadata;
        var other = Route(independent, "/scope/fresh").Metadata;
        Assert.NotSame(first.GetMetadata<TagsAttribute>(), second.GetMetadata<TagsAttribute>());
        Assert.NotSame(first.GetMetadata<TagsAttribute>(), other.GetMetadata<TagsAttribute>());
        Assert.NotSame(first.GetMetadata<EndpointGroupNameAttribute>(), second.GetMetadata<EndpointGroupNameAttribute>());
        Assert.NotSame(first.GetMetadata<EndpointGroupNameAttribute>(), other.GetMetadata<EndpointGroupNameAttribute>());
        Assert.NotSame(first.GetMetadata<ExcludeFromDescriptionAttribute>(), second.GetMetadata<ExcludeFromDescriptionAttribute>());
        Assert.NotSame(first.GetMetadata<ExcludeFromDescriptionAttribute>(), other.GetMetadata<ExcludeFromDescriptionAttribute>());
        var firstTags = Assert.IsAssignableFrom<IList<string>>(first.GetMetadata<TagsAttribute>()!.Tags);
        var secondTags = Assert.IsAssignableFrom<IList<string>>(second.GetMetadata<TagsAttribute>()!.Tags);
        var otherTags = Assert.IsAssignableFrom<IList<string>>(other.GetMetadata<TagsAttribute>()!.Tags);
        Assert.NotSame(firstTags, secondTags);
        Assert.NotSame(firstTags, otherTags);
        firstTags[0] = "mutated-first-mount";
        Assert.Equal(new[] { DocumentationTags.Catalog, DocumentationTags.Read }, secondTags);
        Assert.Equal(new[] { DocumentationTags.Catalog, DocumentationTags.Read }, otherTags);
        Assert.Equal(DocumentationGroups.Public, other.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
        Assert.True(other.GetMetadata<IExcludeFromDescriptionMetadata>()!.ExcludeFromDescription);
    }

    [Fact]
    public async Task Explicit_empty_tags_remain_distinct_from_absent_metadata_and_retain_native_provider_failure()
    {
        var errors = new List<Exception>();
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(mapEndpoints: false);
            if (native)
            {
                var group = app.MapGroup("");
                group.WithMetadata(new TagsAttribute(Array.Empty<string>()), new EndpointGroupNameAttribute(DocumentationGroups.Public));
                group.MapGet("/scope/empty-tags", (DocumentationProbe probe) => TypedResults.Ok(probe.Record("empty-tags"))).WithName("EmptyTags");
            }
            else
            {
                app.MapMediumApiEndpoint<EmptyTagsEndpoint>();
            }

            app.MapGet("/scope/no-tags", () => TypedResults.Ok(new DocumentationResponse("no-tags")));
            Assert.Empty(Route(app, "/scope/empty-tags").Metadata.GetMetadata<ITagsMetadata>()!.Tags);
            Assert.Null(Route(app, "/scope/no-tags").Metadata.GetMetadata<ITagsMetadata>());
            await app.StartAsync();
            Assert.Equal(2, ApiDescriptions(app).Length);
            var error = Record.Exception(() => OpenApi(app, DocumentationGroups.Public));
            Assert.NotNull(error);
            Assert.IsType<InvalidOperationException>(error.GetBaseException());
            errors.Add(error.GetBaseException());
            using var client = app.GetTestClient();
            Assert.Equal(new DocumentationResponse("empty-tags"), await Read(client, "/scope/empty-tags"));
        }

        Assert.Equal(errors[0].Message, errors[1].Message);
    }

    [Fact]
    public async Task Route_description_and_full_document_capture_do_not_activate_endpoint_instances()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiEndpoint<ActivationEndpoint>();
        Assert.Equal(0, Probe(app).Calls);
        Assert.Single(Routes(app));
        await app.StartAsync();
        Assert.Single(Descriptions(app).Operations);
        Assert.Equal(new[] { "/scope/activation" }, Paths(OpenApi(app, DocumentationGroups.Public)));
        Assert.Empty(Paths(OpenApi(app, DocumentationGroups.Admin)));
        Assert.Equal(0, Probe(app).Calls);
    }

    [Fact]
    public async Task Manifests_and_skipped_manual_or_disabled_automatic_endpoints_do_not_materialize_class_metadata()
    {
        await using var app = Create(mapEndpoints: false);
        var manifest = EndpointRegistry.GetManifest(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(ManualThrowingEndpoint) && endpoint.IsManuallyMapped);
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(DisabledEndpoint));
        Assert.Contains(manifest.Endpoints, endpoint => endpoint.EndpointType == typeof(DisabledChildEndpoint));
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<ManualThrowingEndpoint>());
        Assert.Empty(Routes(app));
        await app.StartAsync();
        Assert.Empty(Descriptions(app).Operations);
        Assert.Empty(Paths(OpenApi(app, DocumentationGroups.Public)));
        Assert.Equal(0, Probe(app).Calls);
    }

    [Fact]
    public async Task Disabled_typed_mounts_skip_descendant_conditions_mapping_callbacks_and_metadata()
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
    public async Task Explicit_aliases_keep_class_metadata_without_importing_namespace_visibility_or_conditions()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ScopedGroupDefinition>().MapMediumApiEndpoint<ScopedEndpoint>();
        app.MapMediumApiEndpoint<ScopedEndpoint>("/scope/alias").WithName("ScopedAlias");
        Assert.Null(app.TryMapMediumApiNamespace<DisabledGroupDefinition>());
        app.MapMediumApiEndpoint<DisabledAliasEndpoint>("/scope/disabled-alias").WithName("DisabledAlias");
        var scoped = Route(app, "/scope/scoped/value").Metadata;
        var alias = Route(app, "/scope/alias").Metadata;
        Assert.True(scoped.GetMetadata<IExcludeFromDescriptionMetadata>()!.ExcludeFromDescription);
        Assert.Null(alias.GetMetadata<IExcludeFromDescriptionMetadata>());
        Assert.Equal(new[] { DocumentationTags.Group, DocumentationTags.Catalog }, scoped.GetOrderedMetadata<ITagsMetadata>().Select(value => Assert.Single(value.Tags)));
        Assert.Equal(DocumentationTags.Catalog, Assert.Single(alias.GetMetadata<ITagsMetadata>()!.Tags));
        Assert.Equal(DocumentationGroups.Public, alias.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
        Assert.Null(Route(app, "/scope/disabled-alias").Metadata.GetMetadata<IExcludeFromDescriptionMetadata>());
        await app.StartAsync();
        Assert.Equal(new[] { "/scope/alias", "/scope/disabled-alias" }, Paths(OpenApi(app, DocumentationGroups.Public)));
        Assert.Empty(Paths(OpenApi(app, DocumentationGroups.Admin)));
        using var client = app.GetTestClient();
        Assert.Equal(new DocumentationResponse("scoped"), await Read(client, "/scope/scoped/value"));
        Assert.Equal(new DocumentationResponse("scoped"), await Read(client, "/scope/alias"));
        Assert.Equal(new DocumentationResponse("disabled-alias"), await Read(client, "/scope/disabled-alias"));
    }

    [Fact]
    public async Task Direct_child_mounts_keep_class_metadata_without_reconstructing_parent_prefixes_or_exclusion()
    {
        await using var app = Create(mapEndpoints: false);
        app.MapMediumApiNamespace<ChildGroupDefinition>().MapMediumApiEndpoint<ChildEndpoint>();
        var route = Assert.Single(Routes(app));
        Assert.Equal("/scope-child/value", route.RoutePattern.RawText);
        Assert.Equal(DocumentationTags.Catalog, Assert.Single(route.Metadata.GetMetadata<ITagsMetadata>()!.Tags));
        Assert.Equal(DocumentationGroups.Public, route.Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
        Assert.Null(route.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>());
        await app.StartAsync();
        Assert.Equal(new[] { "/scope-child/value" }, Paths(OpenApi(app, DocumentationGroups.Public)));
        using var client = app.GetTestClient();
        Assert.Equal(new DocumentationResponse("child"), await Read(client, "/scope-child/value"));
        using var absentAncestor = await client.GetAsync("/scope-parent/scope-child/value");
        Assert.Equal(HttpStatusCode.NotFound, absentAncestor.StatusCode);
    }

    [Fact]
    public async Task Class_documentation_metadata_does_not_register_API_Explorer_or_a_document_provider()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(EndpointDocumentationScopeTests).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<DocumentationProbe>();
        await using var app = builder.Build();
        app.MapMediumApiEndpoint<IgnoredAttributesEndpoint>();
        await app.StartAsync();
        Assert.Null(app.Services.GetService<IApiDescriptionGroupCollectionProvider>());
        Assert.Null(app.Services.GetService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>());
        Assert.Equal(DocumentationGroups.Public, Assert.Single(Routes(app)).Metadata.GetMetadata<IEndpointGroupNameMetadata>()!.EndpointGroupName);
        using var client = app.GetTestClient();
        Assert.Equal(new DocumentationResponse("ignored-attributes"), await Read(client, "/scope/ignored-attributes"));
    }

    [Fact]
    public async Task Later_native_false_exclusion_restores_a_class_excluded_description_and_preserves_HTTP_behavior()
    {
        await using var native = Create(mapEndpoints: false);
        await using var generated = Create(mapEndpoints: false);
        var classGroup = native.MapGroup("");
        classGroup.WithMetadata(new TagsAttribute(DocumentationTags.Catalog, DocumentationTags.Read), new EndpointGroupNameAttribute(DocumentationGroups.Public), new ExcludeFromDescriptionAttribute());
        classGroup.MapGet("/scope/fresh", (DocumentationProbe probe) => TypedResults.Ok(probe.Record("fresh"))).WithName("FreshMetadata").WithMetadata(new VisibilityMetadata(false));
        generated.MapMediumApiEndpoint<FreshMetadataEndpoint>().WithMetadata(new VisibilityMetadata(false));
        await native.StartAsync();
        await generated.StartAsync();
        Assert.False(Route(generated, "/scope/fresh").Metadata.GetMetadata<IExcludeFromDescriptionMetadata>()!.ExcludeFromDescription);
        Assert.Single(Route(generated, "/scope/fresh").Metadata.GetOrderedMetadata<ExcludeFromDescriptionAttribute>());
        Assert.Single(ApiDescriptions(generated));
        Equivalent(MediumApiContractComparison.CompareOpenApi(ExampleProgram.GetOpenApiJson(native.Services, DocumentationGroups.Public), ExampleProgram.GetOpenApiJson(generated.Services, DocumentationGroups.Public)));
        Assert.Equal(new[] { "/scope/fresh" }, Paths(OpenApi(generated, DocumentationGroups.Public)));
        using var client = generated.GetTestClient();
        Assert.Equal(new DocumentationResponse("fresh"), await Read(client, "/scope/fresh"));
    }

    [Tags("scope-method")]
    [EndpointGroupName("scope-method")]
    [ExcludeFromDescription]
    private static ScopeMetadataResult NativeOrdered() => new();
}