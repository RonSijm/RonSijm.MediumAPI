using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RonSijm.MediumAPI.NativeFiltersExample.NamedFilterRoutes;

namespace RonSijm.MediumAPI.NativeFiltersExample.IntegrationTest;

internal static class NamedFilterAliasHost
{
    internal static WebApplication Create(string reference = "named", bool enabled = true, bool alias = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing", ContentRootPath = AppContext.BaseDirectory });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Sample:NamedFilterAliases"] = enabled.ToString() });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<NamedFilterProbe>();
        builder.Services.AddScoped<NamedFilterScope>();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
        var app = builder.Build();
        if (reference == "named")
        {
            var group = app.TryMapMediumApiNamespace<NamedFilterGroupDefinition>();
            if (alias && group is not null) { group.MapMediumApiEndpoint<GetNamedFilterValueEndpoint>("/alias/{value:int}").WithName("NamedFilterAlias"); }
        }
        else if (enabled)
        {
            var group = app.MapGroup("/named-filters").WithTags("Named filter aliases");
            if (reference == "generic")
            {
                group.AddMediumApiFilter<AliasGroupGuardFilter>();
                group.WithResponseHeader("X-Named-Group", "registered");
                MapGeneric<GenericNamedValueEndpoint>(group, "/value/{value:int}", "GetNamedFilterValue");
                MapGeneric<GenericNamedRepeatedEndpoint>(group, "/repeated/{value:int}", "GetRepeatedNamedFilter");
                MapGeneric<GenericNamedInheritedEndpoint>(group, "/inherited/{value:int}", "GetInheritedNamedFilter");
                MapGeneric<GenericNamedOverriddenEndpoint>(group, "/overridden/{value:int}", "GetOverriddenNamedFilter");
                if (alias) { MapGeneric<GenericNamedValueEndpoint>(group, "/alias/{value:int}", "NamedFilterAlias"); }
            }
            else
            {
                group.WithMetadata(new EndpointResponseAttribute(400, typeof(NamedFilterError)));
                group.AddEndpointFilter<AliasGroupGuardFilter>();
                group.WithResponseHeader("X-Named-Group", "registered");
                MapNative(group, "/value/{value:int}", "GetNamedFilterValue", configured: true, count: 1);
                MapNative(group, "/repeated/{value:int}", "GetRepeatedNamedFilter", configured: true, count: 2);
                MapNative(group, "/inherited/{value:int}", "GetInheritedNamedFilter", configured: false, count: Arguments(typeof(GenericNamedInheritedEndpoint)));
                MapNative(group, "/overridden/{value:int}", "GetOverriddenNamedFilter", configured: false, count: Arguments(typeof(GenericNamedOverriddenEndpoint)));
                if (alias) { MapNative(group, "/alias/{value:int}", "NamedFilterAlias", configured: true, count: 1); }
            }
        }

        return app;
    }

    private static void MapGeneric<T>(RouteGroupBuilder group, string pattern, string name) where T : class, IGetEndpoint
    {
        EndpointHandlerAdapter.Map<T>(group, pattern, "GET").WithName(name);
    }

    private static void MapNative(RouteGroupBuilder group, string pattern, string name, bool configured, int count)
    {
        var endpoint = group.MapGet(pattern, (Delegate)(Func<int, int?, NamedFilterScope, NamedFilterProbe, HttpContext, Ok<NamedFilterReply>>)NativeRead).WithName(name);
        if (configured)
        {
            endpoint.WithMetadata(new NamedFilterConfiguredMetadata("configured"));
            endpoint.WithMetadata(new EndpointResponseAttribute(404));
            endpoint.WithResponseHeader("X-Named-Configured", "registered");
            endpoint.AddEndpointFilter<AliasConfiguredReadFilter>();
        }

        for (var index = 0; index < count; index++) { endpoint.AddEndpointFilter<AliasArgumentFilter>(); }
    }

    private static Ok<NamedFilterReply> NativeRead([FromRoute] int value, [FromQuery] int? page, NamedFilterScope scope, NamedFilterProbe probe, HttpContext context)
    {
        context.Response.Headers["X-Named-Handler"] = "entry";
        return NamedFilterBodies.Read(value, page, scope, probe, context);
    }

    internal static int Arguments(Type endpoint)
    {
        var declarations = endpoint.GetMethod("HandleAsync")!.GetCustomAttributes(inherit: true).OfType<EndpointFilterAttribute>().ToArray();
        if (declarations.Any(declaration => Target(declaration.GetType()) != typeof(AliasArgumentFilter))) { throw new InvalidOperationException("The native reference requires only the existing argument filter declarations."); }

        return declarations.Length;
    }

    internal static Type Target(Type declaration)
    {
        for (var current = declaration; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(EndpointFilterAttribute<>)) { return current.GenericTypeArguments[0]; }
        }

        throw new InvalidOperationException("The declaration has no fixed native filter target.");
    }

    internal static IEnumerable<RouteEndpoint> Routes(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>();
    }

    internal static RouteEndpoint Route(WebApplication app, string pattern)
    {
        return Routes(app).Single(endpoint => endpoint.RoutePattern.RawText == pattern);
    }

    internal static MediumApiDescriptionSnapshot Descriptions(WebApplication app)
    {
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
    }

    internal static NamedFilterProbe Probe(WebApplication app)
    {
        return app.Services.GetRequiredService<NamedFilterProbe>();
    }
}

[EndpointFilter<AliasConfiguredReadFilter>]
public sealed class GenericNamedValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value/{value:int}";

    [EndpointFilter<AliasArgumentFilter>]
    [ResponseHeader("X-Named-Handler", "entry")]
    public Ok<NamedFilterReply> HandleAsync([FromRoute] int value, [FromQuery] int? page, NamedFilterScope scope, NamedFilterProbe probe, HttpContext context)
    {
        return NamedFilterBodies.Read(value, page, scope, probe, context);
    }
}

[EndpointFilter<AliasConfiguredReadFilter>]
public sealed class GenericNamedRepeatedEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/repeated/{value:int}";

    [EndpointFilter<AliasArgumentFilter>]
    [EndpointFilter<AliasArgumentFilter>]
    [ResponseHeader("X-Named-Handler", "entry")]
    public Ok<NamedFilterReply> HandleAsync([FromRoute] int value, [FromQuery] int? page, NamedFilterScope scope, NamedFilterProbe probe, HttpContext context)
    {
        return NamedFilterBodies.Read(value, page, scope, probe, context);
    }
}

[EndpointFilter<AliasConfiguredReadFilter>]
public abstract class GenericNamedMethodBase : IGetEndpoint
{
    public abstract string Pattern { get; }

    [EndpointFilter<AliasArgumentFilter>]
    [ResponseHeader("X-Named-Handler", "entry")]
    public virtual Ok<NamedFilterReply> HandleAsync([FromRoute] int value, [FromQuery] int? page, NamedFilterScope scope, NamedFilterProbe probe, HttpContext context)
    {
        return NamedFilterBodies.Read(value, page, scope, probe, context);
    }
}

public sealed class GenericNamedInheritedEndpoint : GenericNamedMethodBase
{
    [StringSyntax("Route")]
    public override string Pattern => "/inherited/{value:int}";
}

public sealed class GenericNamedOverriddenEndpoint : GenericNamedMethodBase
{
    [StringSyntax("Route")]
    public override string Pattern => "/overridden/{value:int}";

    [EndpointFilter<AliasArgumentFilter>]
    [ResponseHeader("X-Named-Handler", "entry")]
    public override Ok<NamedFilterReply> HandleAsync([FromRoute] int value, [FromQuery] int? page, NamedFilterScope scope, NamedFilterProbe probe, HttpContext context)
    {
        return NamedFilterBodies.Read(value, page, scope, probe, context);
    }
}