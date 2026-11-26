using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static RonSijm.MediumAPI.ConstructorBindingExample.IntegrationTest.ConstructorBindingHost;

namespace RonSijm.MediumAPI.ConstructorBindingExample.IntegrationTest;

public sealed class ConstructorBindingTests
{
    [Fact]
    public async Task Aggregate_handlers_retain_independent_native_route_and_API_Explorer_contracts()
    {
        await using var native = Create(native: true);
        await using var generated = Create();
        await native.StartAsync();
        await generated.StartAsync();
        Equivalent(MediumApiContractComparison.Compare(native.GetMediumApiContractSnapshot(), generated.GetMediumApiContractSnapshot()));
        Equivalent(MediumApiContractComparison.CompareApiDescriptions(Descriptions(native), Descriptions(generated)));
        Assert.Equal(2, Routes(generated).Count());
        Assert.Equal(2, Descriptions(generated).Operations.Count);
        Assert.Equal(0, Probe(native).Calls);
        Assert.Equal(0, Probe(generated).Calls);
    }

    [Theory]
    [InlineData("GetLocalItem", typeof(LocalItemRequest), 3)]
    [InlineData("GetWorkspaceItem", typeof(WorkspaceItemRequest), 4)]
    public async Task Original_method_and_flattened_description_metadata_keep_explicit_member_sources(string name, Type aggregateType, int describedParameters)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var parameters = NamedRoute(app, name).Metadata.GetMetadata<MethodInfo>()!.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal(aggregateType, parameters[0].ParameterType);
            Assert.Single(parameters[0].GetCustomAttributes<AsParametersAttribute>());
            Assert.Equal(typeof(HttpContext), parameters[1].ParameterType);
            var description = Assert.Single(Descriptions(app).Operations, operation => operation.Name == name);
            Assert.Equal(describedParameters, description.Parameters.Count);
            Assert.Equal("Path", Assert.Single(description.Parameters, parameter => parameter.Name == "itemId").Source);
            Assert.True(Assert.Single(description.Parameters, parameter => parameter.Name == "itemId").IsRequired);
            Assert.Equal("Query", Assert.Single(description.Parameters, parameter => parameter.Name == "page").Source);
            Assert.False(Assert.Single(description.Parameters, parameter => parameter.Name == "page").IsRequired);
            Assert.Equal("Header", Assert.Single(description.Parameters, parameter => parameter.Name == SampleMetadata.LabelHeader).Source);
            Assert.False(Assert.Single(description.Parameters, parameter => parameter.Name == SampleMetadata.LabelHeader).IsRequired);
            Assert.DoesNotContain(description.Parameters, parameter => parameter.Name is "request" or "context" or "Service");
            if (aggregateType == typeof(WorkspaceItemRequest))
            {
                Assert.Equal("Path", Assert.Single(description.Parameters, parameter => parameter.Name == "customerId").Source);
            }
        }
    }

    [Theory]
    [InlineData(typeof(LocalItemRequest), true)]
    [InlineData(typeof(WorkspaceItemRequest), false)]
    public void Native_constructor_metadata_keeps_parameter_and_property_sources_distinct_and_preserves_defaults(Type aggregateType, bool constructorAttributes)
    {
        var constructor = Assert.Single(aggregateType.GetConstructors());
        Assert.NotEmpty(constructor.GetParameters());
        var selectedItem = Assert.Single(constructor.GetParameters(), parameter => parameter.Name!.Equals("SelectedItem", StringComparison.OrdinalIgnoreCase));
        var property = aggregateType.GetProperty("SelectedItem")!;
        Assert.Equal(property.PropertyType, selectedItem.ParameterType);
        Assert.Equal(constructorAttributes ? "itemId" : null, selectedItem.GetCustomAttribute<FromRouteAttribute>()?.Name);
        Assert.Equal(constructorAttributes ? null : "itemId", property.GetCustomAttribute<FromRouteAttribute>()?.Name);
        var page = Assert.Single(constructor.GetParameters(), parameter => parameter.Name!.Equals("Page", StringComparison.OrdinalIgnoreCase));
        Assert.True(page.HasDefaultValue);
        Assert.Equal(SampleMetadata.DefaultPage, page.DefaultValue);
        var label = Assert.Single(constructor.GetParameters(), parameter => parameter.Name!.Equals("Label", StringComparison.OrdinalIgnoreCase));
        Assert.True(label.HasDefaultValue);
        Assert.Null(label.DefaultValue);
        var service = Assert.Single(constructor.GetParameters(), parameter => parameter.Name!.Equals("Service", StringComparison.OrdinalIgnoreCase));
        Assert.False(service.HasDefaultValue);
        Assert.Equal(constructorAttributes, service.GetCustomAttribute<FromServicesAttribute>() is not null);
        Assert.Equal(!constructorAttributes, aggregateType.GetProperty("Service")!.GetCustomAttribute<FromServicesAttribute>() is not null);
        Assert.All(constructor.GetParameters(), parameter => Assert.Contains(aggregateType.GetProperties(), candidate => candidate.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase) && candidate.PropertyType == parameter.ParameterType));
    }

    [Theory]
    [InlineData("/items/42", "local", 42L, null)]
    [InlineData("/items/9223372036854775807", "local", long.MaxValue, null)]
    [InlineData("/customers/2147483648/items/9223372036854775807", "workspace", long.MaxValue, 2147483648L)]
    [InlineData("/customers/-1/items/0", "workspace", 0L, -1L)]
    public async Task Local_and_inherited_aliases_keep_long_values_constructor_defaults_and_injected_services(string path, string kind, long itemId, long? customerId)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new AggregateResponse(kind, itemId, customerId, SampleMetadata.DefaultPage, null, SampleMetadata.ServiceName, true, 2), await Read(client, path));
            Assert.Equal(1, Probe(app).Calls);
        }
    }

    [Theory]
    [InlineData("/items/42", "local", null)]
    [InlineData("/customers/5/items/42", "workspace", 5L)]
    public async Task Explicit_member_sources_reject_query_attempts_to_replace_route_header_or_service_values(string path, string kind, long? customerId)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var requestPath = path + "?itemId=99&SelectedItem=98&customerId=97&AccountId=96&page=7&Label=query&Service=spoof";
            Assert.Equal(new AggregateResponse(kind, 42, customerId, 7, "header", SampleMetadata.ServiceName, true, 2), await Read(client, requestPath, "header"));
        }
    }

    [Theory]
    [InlineData("/items/42")]
    [InlineData("/customers/5/items/42")]
    public async Task Query_values_do_not_fill_a_missing_explicit_header_property(string path)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var response = await Read(client, path + "?Label=query&X-Sample-Label=query&page=0");
            Assert.Null(response.Label);
            Assert.Equal(0, response.Page);
        }
    }

    [Theory]
    [InlineData("/items/42?page=invalid")]
    [InlineData("/items/42?page=2147483648")]
    [InlineData("/customers/5/items/42?page=invalid")]
    [InlineData("/customers/5/items/42?page=2147483648")]
    public async Task Invalid_or_overflowed_query_members_keep_native_bad_request_without_handler_dispatch(string path)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Theory]
    [InlineData("/items/not-a-long")]
    [InlineData("/items/9223372036854775808")]
    [InlineData("/customers/not-a-long/items/42")]
    [InlineData("/customers/9223372036854775808/items/42")]
    [InlineData("/customers/5/items/not-a-long")]
    [InlineData("/customers/5/items/9223372036854775808")]
    public async Task Native_route_constraints_reject_invalid_or_overflowed_local_and_parent_members(string path)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, Probe(app).Calls);
        }
    }

    [Theory]
    [InlineData("GetLocalItem", null, "/items/2147483648")]
    [InlineData("GetWorkspaceItem", 9223372036854775807L, "/customers/9223372036854775807/items/2147483648")]
    public async Task Named_links_keep_route_alias_names_and_parent_values(string name, long? customerId, string expected)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var values = new RouteValueDictionary { ["itemId"] = 2147483648L };
            if (customerId.HasValue)
            {
                values["customerId"] = customerId.Value;
            }

            Assert.Equal(expected, app.Services.GetRequiredService<LinkGenerator>().GetPathByName(name, values));
        }
    }

    [Fact]
    public async Task Service_properties_resolve_the_selected_hosts_registered_service()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native, serviceName: "configured-service");
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal("configured-service", (await Read(client, "/items/42?Service=spoof")).ServiceName);
            Assert.Equal("configured-service", (await Read(client, "/customers/5/items/42?Service=spoof")).ServiceName);
        }
    }

    [Fact]
    public async Task Filters_and_handlers_receive_the_same_original_aggregate_object_and_original_argument_count()
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var local = await Read(client, "/items/42?page=3", "local-label");
            var workspace = await Read(client, "/customers/5/items/42?page=4", "workspace-label");
            Assert.True(local.OriginalAggregate);
            Assert.True(workspace.OriginalAggregate);
            Assert.Equal(2, local.ArgumentCount);
            Assert.Equal(2, workspace.ArgumentCount);
            Assert.Equal(3, local.Page);
            Assert.Equal(4, workspace.Page);
            Assert.Equal(2, Probe(app).Calls);
        }
    }

    [Theory]
    [InlineData("/items")]
    [InlineData("/customers/5/items")]
    [InlineData("/customers/items/42")]
    public async Task Missing_required_route_members_do_not_create_additional_routes(string path)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, Probe(app).Calls);
        }
    }
}