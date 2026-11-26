using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static RonSijm.MediumAPI.StructBindingExample.IntegrationTest.StructBindingHost;

namespace RonSijm.MediumAPI.StructBindingExample.IntegrationTest;

public sealed class StructBindingTests
{
    [Fact]
    public async Task Struct_handlers_match_independent_native_route_and_API_Explorer_contracts()
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
    [InlineData("GetLocalStructItem", typeof(LocalItemRequest), 3)]
    [InlineData("GetWorkspaceStructItem", typeof(WorkspaceItemRequest), 4)]
    public async Task Original_value_type_signature_and_native_member_sources_are_preserved(string name, Type aggregateType, int describedParameters)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            var parameters = NamedRoute(app, name).Metadata.GetMetadata<MethodInfo>()!.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal(aggregateType, parameters[0].ParameterType);
            Assert.True(parameters[0].ParameterType.IsValueType);
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
            Assert.DoesNotContain(description.Parameters, parameter => parameter.Name is "request" or "context" or "Service" or "ParameterlessConstructorUsed");
            if (aggregateType == typeof(WorkspaceItemRequest))
            {
                Assert.Equal("Path", Assert.Single(description.Parameters, parameter => parameter.Name == "customerId").Source);
            }
        }
    }

    [Fact]
    public async Task Native_constructor_selection_preserves_default_struct_creation_and_prefers_an_explicit_parameterless_constructor()
    {
        Assert.Empty(typeof(LocalItemRequest).GetConstructors());
        var constructors = typeof(WorkspaceItemRequest).GetConstructors();
        Assert.Equal(2, constructors.Length);
        Assert.Contains(constructors, constructor => constructor.GetParameters().Length == 0);
        Assert.Contains(constructors, constructor => constructor.GetParameters().Length == 1);
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.False((await Read(client, "/items/42")).ParameterlessConstructorUsed);
            var workspace = await Read(client, "/customers/5/items/42");
            Assert.True(workspace.ParameterlessConstructorUsed);
            Assert.Null(workspace.Page);
            Assert.Null(workspace.Label);
            var description = Assert.Single(app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items), operation => operation.ActionDescriptor.EndpointMetadata.OfType<IEndpointNameMetadata>().Any(metadata => metadata.EndpointName == "GetWorkspaceStructItem"));
            Assert.Null(Assert.Single(description.ParameterDescriptions, parameter => parameter.Name == "page").DefaultValue);
            Assert.Null(Assert.Single(description.ParameterDescriptions, parameter => parameter.Name == SampleMetadata.LabelHeader).DefaultValue);
        }
    }

    [Theory]
    [InlineData("/items/9223372036854775807", "local", long.MaxValue, null, false)]
    [InlineData("/customers/2147483648/items/9223372036854775807", "workspace", long.MaxValue, 2147483648L, true)]
    [InlineData("/customers/-1/items/0", "workspace", 0L, -1L, true)]
    public async Task Struct_aliases_keep_long_values_optional_nulls_boxed_filter_values_and_service_identity(string path, string kind, long itemId, long? customerId, bool constructorUsed)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new AggregateResponse(kind, itemId, customerId, null, null, SampleMetadata.ServiceName, true, true, 2, constructorUsed), await Read(client, path));
            Assert.Equal(1, Probe(app).Calls);
        }
    }

    [Theory]
    [InlineData("/items/42", "local", null, false)]
    [InlineData("/customers/5/items/42", "workspace", 5L, true)]
    public async Task Explicit_sources_keep_route_aliases_header_and_service_outside_query_replacement(string path, string kind, long? customerId, bool constructorUsed)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            Assert.Equal(new AggregateResponse(kind, 42, customerId, 7, "header", SampleMetadata.ServiceName, true, true, 2, constructorUsed), await Read(client, path + "?itemId=99&SelectedItem=98&customerId=97&AccountId=96&page=7&Label=query&Service=spoof", "header"));
        }
    }

    [Theory]
    [InlineData("/items/42")]
    [InlineData("/customers/5/items/42")]
    public async Task Query_values_do_not_fill_missing_explicit_header_properties(string path)
    {
        foreach (var native in new[] { true, false })
        {
            await using var app = Create(native);
            await app.StartAsync();
            using var client = app.GetTestClient();
            var response = await Read(client, path + "?Label=query&X-Sample-Label=query&page=0");
            Assert.Null(response.Label);
            Assert.Equal(0, response.Page);
            Assert.True(response.FilterValueMatches);
            Assert.True(response.FilterServiceMatches);
            Assert.Equal(2, response.ArgumentCount);
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
    public async Task Local_and_parent_constraints_retain_native_not_found_boundaries(string path)
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
    [InlineData("GetLocalStructItem", null, "/items/2147483648")]
    [InlineData("GetWorkspaceStructItem", 9223372036854775807L, "/customers/9223372036854775807/items/2147483648")]
    public async Task Named_links_keep_explicit_property_aliases_and_parent_values(string name, long? customerId, string expected)
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
    public async Task Independent_hosts_resolve_their_own_service_and_probe_for_struct_properties()
    {
        foreach (var native in new[] { true, false })
        {
            await using var first = Create(native, serviceName: "first-host");
            await using var second = Create(native, serviceName: "second-host");
            await first.StartAsync();
            await second.StartAsync();
            using var firstClient = first.GetTestClient();
            using var secondClient = second.GetTestClient();
            var firstResponse = await Read(firstClient, "/items/42?Service=spoof");
            var secondResponse = await Read(secondClient, "/customers/5/items/42?Service=spoof");
            Assert.Equal("first-host", firstResponse.ServiceName);
            Assert.Equal("second-host", secondResponse.ServiceName);
            Assert.True(firstResponse.FilterServiceMatches);
            Assert.True(secondResponse.FilterServiceMatches);
            Assert.Equal(1, Probe(first).Calls);
            Assert.Equal(1, Probe(second).Calls);
        }
    }

    [Theory]
    [InlineData("/items")]
    [InlineData("/customers/5/items")]
    [InlineData("/customers/items/42")]
    public async Task Missing_required_members_do_not_add_routes_or_invoke_handlers(string path)
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