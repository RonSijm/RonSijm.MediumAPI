using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.ResourcePoliciesExample.Program;

namespace RonSijm.MediumAPI.ResourcePoliciesExample.IntegrationTest;

internal static class ResourcePoliciesHost
{
    internal static WebApplication Create(bool legacy)
    {
        return ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        }, legacy: legacy);
    }

    internal static ResourceProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<ResourceProbe>();
    }

    internal static Task<HttpResponseMessage> Command(HttpClient client, string action, string outcome)
    {
        return action switch
        {
            "list" => client.GetAsync("/sample/connections?outcome=" + outcome),
            "save" => client.PostAsJsonAsync("/sample/connections", new ResourceCommand(outcome)),
            "delete" => client.DeleteAsync("/sample/connections/7?outcome=" + outcome),
            "queue" => client.PostAsJsonAsync("/sample/connection-jobs", new ResourceCommand(outcome)),
            _ => client.PostAsJsonAsync("/sample/connection-jobs/7/" + action, new ResourceCommand(outcome))
        };
    }

    internal static string[] Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items)
            .Select(description => description.HttpMethod + "|" + description.RelativePath + "|" + string.Join(";", description.SupportedResponseTypes.OrderBy(response => response.StatusCode).Select(response => response.StatusCode + ":" + response.Type?.FullName + ":" + string.Join(",", response.ApiResponseFormats.Select(format => format.MediaType)))))
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    internal static RouteEndpoint Route(WebApplication app, string pattern, string method = "GET")
    {
        return Assert.Single(app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>(), endpoint => endpoint.RoutePattern.RawText == pattern && endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains(method));
    }

    internal static string[] Responses(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(response => response.StatusCode + ":" + response.Type?.FullName + ":" + string.Join(",", response.ContentTypes)).ToArray();
    }
}