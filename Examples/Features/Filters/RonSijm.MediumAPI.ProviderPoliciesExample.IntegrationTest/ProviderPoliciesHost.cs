using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.ProviderPoliciesExample.Program;

namespace RonSijm.MediumAPI.ProviderPoliciesExample.IntegrationTest;

internal static class ProviderPoliciesHost
{
    internal static WebApplication Create(bool legacy = false)
    {
        return ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        }, legacy: legacy);
    }

    internal static ProviderProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<ProviderProbe>();
    }

    internal static async Task<HttpResponseMessage> Send(HttpClient client, string policy, string outcome, bool blocked = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/providers/" + policy + "/7") { Content = JsonContent.Create(new ProviderRequest(outcome)) };
        if (blocked)
        {
            request.Headers.Add("X-Sample-Block", "true");
        }

        return await client.SendAsync(request);
    }

    internal static RouteEndpoint Route(WebApplication app, string pattern)
    {
        return Assert.Single(app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>(), endpoint => endpoint.RoutePattern.RawText == pattern);
    }

    internal static string[] Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items)
            .Select(description => description.HttpMethod + "|" + description.RelativePath + "|" + string.Join(";", description.SupportedResponseTypes.OrderBy(response => response.StatusCode).Select(response => response.StatusCode + ":" + response.Type?.FullName + ":" + string.Join(",", response.ApiResponseFormats.Select(format => format.MediaType)))))
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    internal static ApiDescription Description(WebApplication app, string pattern)
    {
        return Assert.Single(app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items), description => "/" + description.RelativePath == pattern);
    }

    internal static string[] NativeResponses(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(response => response.StatusCode + ":" + response.Type?.FullName + ":" + string.Join(",", response.ContentTypes)).ToArray();
    }
}