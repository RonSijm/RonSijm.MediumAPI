using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RonSijm.MediumAPI.Example.IntegrationTest;

public sealed class EndpointMetadataTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/metadata/items/42")]
    [InlineData("/metadata/items/42?id=99")]
    public async Task Generated_handler_binds_route_parameters(string url)
    {
        using var client = factory.CreateClient();
        var item = await client.GetFromJsonAsync<MetadataItem>(url);
        Assert.Equal(42, item!.Id);
    }

    [Fact]
    public void Generated_endpoint_preserves_handler_metadata_and_named_links()
    {
        using var client = factory.CreateClient();
        var endpoint = Assert.Single(factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>(), endpoint => endpoint.RoutePattern.RawText == "metadata/items/{id:int}");

        Assert.Equal("MetadataEndpoint", endpoint.Metadata.GetMetadata<MethodInfo>()!.DeclaringType!.Name);
        Assert.Equal("GetMetadataItem", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal("GetMetadataItem", endpoint.Metadata.GetMetadata<IRouteNameMetadata>()!.RouteName);
        Assert.Equal("Metadata", Assert.Single(endpoint.Metadata.GetMetadata<ITagsMetadata>()!.Tags));
        var responses = endpoint.Metadata.GetOrderedMetadata<ProducesResponseTypeAttribute>();
        Assert.Contains(responses, response => response.StatusCode == 200 && response.Type == typeof(MetadataItem));
        Assert.Contains(responses, response => response.StatusCode == 404);
        Assert.Equal("/metadata/items/42", factory.Services.GetRequiredService<LinkGenerator>()
            .GetPathByName("GetMetadataItem", new { id = 42 }));
    }

    [Fact]
    public void ApiExplorer_uses_the_original_handler_signature_and_response_contracts()
    {
        using var client = factory.CreateClient();
        var descriptions = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items.SelectMany(group => group.Items);
        var description = Assert.Single(descriptions, item => item.ActionDescriptor.EndpointMetadata
            .OfType<IEndpointNameMetadata>().Any(metadata => metadata.EndpointName == "GetMetadataItem"));
        var parameter = Assert.Single(description.ParameterDescriptions);

        Assert.Equal("id", parameter.Name);
        Assert.Equal(typeof(int), parameter.Type);
        Assert.Equal(BindingSource.Path, parameter.Source);
        Assert.Contains(description.SupportedResponseTypes, response => response.StatusCode == 200 && response.Type == typeof(MetadataItem));
        Assert.Contains(description.SupportedResponseTypes, response => response.StatusCode == 404);
    }
}