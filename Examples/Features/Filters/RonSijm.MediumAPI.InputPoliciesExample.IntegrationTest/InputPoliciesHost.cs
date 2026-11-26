using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.InputPoliciesExample.Program;

namespace RonSijm.MediumAPI.InputPoliciesExample.IntegrationTest;

internal static class InputPoliciesHost
{
    internal static WebApplication Create(bool legacy = false)
    {
        return ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        }, legacy: legacy);
    }

    internal static InputProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<InputProbe>();
    }

    internal static async Task<HttpResponseMessage> Send(HttpClient client, string kind, string outcome, bool blocked = false)
    {
        var path = kind == "membership" ? "/sample/members/7/invitations" : kind == "intake" ? "/sample/intake/7" : "/sample/intake/7/report?outcome=" + outcome;
        using var request = new HttpRequestMessage(kind == "report" ? HttpMethod.Get : HttpMethod.Post, path);
        if (kind != "report")
        {
            request.Content = JsonContent.Create(new InputRequest(outcome));
        }

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