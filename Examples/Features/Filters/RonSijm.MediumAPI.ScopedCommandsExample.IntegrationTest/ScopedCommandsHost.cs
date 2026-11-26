using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.ScopedCommandsExample.Program;

namespace RonSijm.MediumAPI.ScopedCommandsExample.IntegrationTest;

internal static class ScopedCommandsHost
{
    internal static WebApplication Create(bool legacy = false, OwnershipPause? pause = null)
    {
        return ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            if (pause is not null)
            {
                builder.Services.AddSingleton(pause);
            }
        }, legacy: legacy);
    }

    internal static CommandProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<CommandProbe>();
    }

    internal static Task<HttpResponseMessage> Send(HttpClient client, string kind, string scope, string outcome, bool blocked = false)
    {
        return SendCore(client, kind, scope, outcome, blocked);
    }

    private static async Task<HttpResponseMessage> SendCore(HttpClient client, string kind, string scope, string outcome, bool blocked)
    {
        var path = kind == "release" ? "/commands/releases/7" : "/commands/projects/3/resources/7" + (kind == "review" ? "/review" : "");
        path += "?scope=" + scope;
        if (kind == "read")
        {
            path += "&operation=" + outcome;
        }

        using var request = new HttpRequestMessage(kind == "read" ? HttpMethod.Get : HttpMethod.Post, path);
        if (kind != "read")
        {
            request.Content = JsonContent.Create(new CommandRequest(outcome));
        }

        if (blocked)
        {
            request.Headers.Add("X-Sample-Block", "true");
        }

        return await client.SendAsync(request);
    }

    internal static RouteEndpoint Route(WebApplication app, string path)
    {
        return Assert.Single(app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>(), route => route.RoutePattern.RawText == path);
    }

    internal static string[] Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(group => group.Items)
            .Select(description => description.HttpMethod + "|" + description.RelativePath + "|" + string.Join(";", description.SupportedResponseTypes.OrderBy(response => response.StatusCode).Select(response => response.StatusCode + ":" + response.Type?.FullName + ":" + string.Join(",", response.ApiResponseFormats.Select(format => format.MediaType)))))
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    internal static string[] NativeResponses(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>().Select(response => response.StatusCode + ":" + response.Type?.FullName + ":" + string.Join(",", response.ContentTypes)).ToArray();
    }
}