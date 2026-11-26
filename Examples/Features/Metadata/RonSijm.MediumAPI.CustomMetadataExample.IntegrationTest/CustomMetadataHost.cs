using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ExampleProgram = RonSijm.MediumAPI.CustomMetadataExample.Program;

namespace RonSijm.MediumAPI.CustomMetadataExample.IntegrationTest;

internal static class CustomMetadataHost
{
    internal static WebApplication Create(bool native = false, bool mapEndpoints = true)
    {
        var app = ExampleProgram.Build(configure: builder =>
        {
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
        }, mapEndpoints: mapEndpoints && !native);
        if (native && mapEndpoints)
        {
            MapNative(app);
        }

        return app;
    }

    internal static MetadataProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<MetadataProbe>();
    }

    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    }

    internal static RouteEndpoint Route(WebApplication app, string path)
    {
        return Assert.Single(Routes(app), endpoint => endpoint.RoutePattern.RawText == path);
    }

    internal static AppMetadataAttribute[] Metadata(WebApplication app, string path)
    {
        return Route(app, path).Metadata.GetOrderedMetadata<AppMetadataAttribute>().ToArray();
    }

    internal static string[] Names(WebApplication app, string path)
    {
        return Metadata(app, path).Select(metadata => metadata.Name).ToArray();
    }

    internal static string Describe(AppMetadataAttribute value)
    {
        return JsonSerializer.Serialize(new { value.Name, Contract = value.Contract?.FullName, BoxedType = value.Boxed?.GetType().FullName, BoxedValue = value.Boxed?.ToString(), Modes = value.Modes?.Select(mode => mode.ToString()).ToArray(), value.Aliases, ExtraType = value.Extra?.GetType().FullName, ExtraValue = value.Extra?.ToString(), value.Optional, value.Flag });
    }

    internal static string[] Inventory(WebApplication app)
    {
        return Routes(app).Select(route => route.RoutePattern.RawText + ":" + string.Join(";", route.Metadata.GetOrderedMetadata<AppMetadataAttribute>().Select(Describe))).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    }

    internal static void Equivalent(MediumApiContractComparison comparison)
    {
        Assert.True(comparison.IsEquivalent, string.Join(Environment.NewLine, comparison.Differences.Select(difference => difference.Kind + " " + difference.Path)));
    }

    private static RouteGroupBuilder ClassMetadata(IEndpointRouteBuilder parent, params object[] values)
    {
        return parent.MapGroup("").WithMetadata(values);
    }

    private static void MapNative(WebApplication app)
    {
        var group = app.MapGroup("/metadata").WithMetadata(new AppMetadataAttribute("map"), new AppMetadataAttribute("group"));
        group.AddEndpointFilter<ReadMetadataFilter>();
        group.MapGet("/native", MetadataReplies.Native).WithName("NativeMetadata");
        var complex = new AppMetadataAttribute("class", typeof(MetadataReply), MetadataMode.Second, [MetadataMode.First, MetadataMode.Second]) { Aliases = ["alpha", "beta"], Extra = typeof(string), Optional = null, Flag = MetadataMode.Second };
        ClassMetadata(group, complex).MapGet("/class", NativeClass).WithName("ClassMetadata");
        group.MapGet("/method", NativeMethod).WithName("MethodMetadata");
        var filtered = ClassMetadata(group, new AllowAnonymousAttribute()).MapGet("/filter", NativeFilter).WithName("FilterMetadata");
        filtered.WithMetadata(new AppMetadataAttribute("filter-configure"), new AppMetadataAttribute("filter"), new SampleAccessAttribute { Policy = "SampleReaders" }, new CustomResponseAttribute());
        ((IEndpointConventionBuilder)filtered).Add(builder =>
        {
            if (!builder.Metadata.OfType<IProducesResponseTypeMetadata>().Any(value => value.StatusCode == 404))
            {
                builder.Metadata.Add(new ProducesResponseTypeMetadata(404));
            }
        });
        filtered.AddEndpointFilter<ConfiguredMetadataFilter>();
        var ordered = ClassMetadata(group, new AppMetadataAttribute("class")).MapGet("/order", NativeOrder).WithName("OrderedMetadata");
        ordered.WithMetadata(new AppMetadataAttribute("endpoint-configure"));
        ((IEndpointConventionBuilder)ordered).Finally(builder => builder.Metadata.Add(new AppMetadataAttribute("endpoint-final")));
        ClassMetadata(group, new AppMetadataAttribute("derived-class")).MapGet("/inherited", NativeInherited).WithName("InheritedMetadata").WithMetadata(new AppMetadataAttribute("derived-filter")).AddEndpointFilter<DerivedMetadataFilter>();
        ClassMetadata(group, new AppMetadataAttribute("first"), new AppMetadataAttribute("second")).MapGet("/repeated", NativeRepeated).WithName("RepeatedMetadata");
        ClassMetadata(group, new AppMetadataAttribute("null", null, null, null) { Aliases = null, Extra = null, Optional = null }).MapGet("/null", NativeNull).WithName("NullMetadata");
        ClassMetadata(group, new StaticMetadataAttribute()).MapGet("/static", NativeStatic).WithName("StaticMetadata");
        ClassMetadata(group, new SampleAccessAttribute { Policy = "SampleReaders" }, new AllowAnonymousAttribute()).MapGet("/dual", NativeDual).WithName("DualMetadata");
        ClassMetadata(app, new AppMetadataAttribute("manual")).MapGet("/aliases/first", NativeAlias).WithName("FirstMetadataAlias");
        ClassMetadata(app, new AppMetadataAttribute("manual")).MapGet("/aliases/second", NativeAlias).WithName("SecondMetadataAlias");
    }

    private static Ok<MetadataReply> NativeClass(HttpContext context)
    {
        return MetadataReplies.Ok("class", context);
    }

    [AppMetadata("method")]
    [UnmarkedMethodMetadata("native-method")]
    private static Ok<MetadataReply> NativeMethod(HttpContext context)
    {
        return MetadataReplies.Ok("method", context);
    }

    private static Ok<MetadataReply> NativeFilter(HttpContext context)
    {
        return MetadataReplies.Ok("filter", context);
    }

    [AppMetadata("method")]
    private static MetadataProviderResult NativeOrder()
    {
        return new MetadataProviderResult();
    }

    [AppMetadata("inherited-method")]
    private static Ok<MetadataReply> NativeInherited(HttpContext context)
    {
        return MetadataReplies.Ok("inherited", context);
    }

    private static Ok<MetadataReply> NativeRepeated(HttpContext context)
    {
        return MetadataReplies.Ok("repeated", context);
    }

    private static Ok<MetadataReply> NativeNull(HttpContext context)
    {
        return MetadataReplies.Ok("null", context);
    }

    private static Ok<MetadataReply> NativeStatic(HttpContext context)
    {
        return MetadataReplies.Ok("static", context);
    }

    private static Ok<MetadataReply> NativeDual(HttpContext context)
    {
        return MetadataReplies.Ok("dual", context);
    }

    private static Ok<MetadataReply> NativeAlias(HttpContext context)
    {
        return MetadataReplies.Ok("alias", context);
    }
}