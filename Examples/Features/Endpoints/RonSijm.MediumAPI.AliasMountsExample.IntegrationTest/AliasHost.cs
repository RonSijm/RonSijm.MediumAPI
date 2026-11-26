using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.AliasMountsExample.Endpoints.Owned;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.AliasMountsExample.Program;

namespace RonSijm.MediumAPI.AliasMountsExample.IntegrationTest;

internal static class AliasHost
{
    internal static WebApplication Create(bool legacy = false, bool map = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        }, mapEndpoints: map && !legacy);
        if (legacy && map)
        {
            MapLegacy(app);
        }

        return app;
    }

    internal static RouteHandlerBuilder LegacyAliases<TEndpoint>(IEndpointRouteBuilder builder, IEnumerable<string> patterns) where TEndpoint : IEndpoint
    {
        var children = new List<IEndpointConventionBuilder>();
        foreach (var pattern in patterns)
        {
            children.Add(builder.MapMediumApiEndpoint<TEndpoint>(pattern).WithoutDefaultEndpointName());
        }

        return new RouteHandlerBuilder(children.ToArray());
    }

    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    }

    internal static RouteEndpoint Route(WebApplication app, string path)
    {
        return Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    }

    internal static AliasProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<AliasProbe>();
    }

    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    }

    internal static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    }

    internal static async Task Authenticate(HttpClient client)
    {
        using var response = await client.GetAsync("/sign-in");
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        var cookies = response.Headers.GetValues("Set-Cookie").Select(value => value.Split(';')[0]);
        client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies));
    }

    private static void MapLegacy(WebApplication app)
    {
        app.MapMediumApiNamespace<OwnedGroupDefinition>();
        ConfigureLegacyAggregate(LegacyAliases<SharedAliasEndpoint>(app, ["/root/first/{id:int}", "/root/second/{id:int}"]), "Root");
        var publicGroup = app.MapGroup("/public").WithResponseHeader("X-Group", "public");
        ConfigureLegacyAggregate(LegacyAliases<SharedAliasEndpoint>(publicGroup, ["/first/{id:int}", "/second/{id:int}"]), "Public");
        var protectedGroup = app.MapGroup("/api").MapGroup("/private").RequireAuthorization("AliasReaders").WithMetadata(new ProducesResponseTypeMetadata(401)).WithResponseHeader("X-Group", "private");
        ConfigureLegacyAggregate(LegacyAliases<SharedAliasEndpoint>(protectedGroup, ["/first/{id:int}", "/second/{id:int}"]), "Private");
        LegacyAliases<NamedAliasEndpoint>(app, ["/named/one"]);
        app.MapGet("/sign-in", (Func<HttpContext, Task<NoContent>>)ExampleProgram.SignIn).WithName("SignInAliasExample");
    }

    private static void ConfigureLegacyAggregate(RouteHandlerBuilder aggregate, string scope)
    {
        aggregate.WithTags("Alias mounts").WithResponseHeader("X-Aggregate", scope).AddMediumApiFilter<AggregateAliasFilter>();
        ((IEndpointConventionBuilder)aggregate).Finally(builder =>
        {
            var path = ((RouteEndpointBuilder)builder).RoutePattern.RawText!;
            var suffix = path.Contains("/first/", StringComparison.Ordinal) ? "First" : "Second";
            builder.Metadata.Add(new EndpointNameMetadata(scope + suffix));
            builder.Metadata.Add(new RouteNameMetadata(scope + suffix));
            builder.Metadata.Add(new AliasFinalMetadata(scope));
        });
    }
}