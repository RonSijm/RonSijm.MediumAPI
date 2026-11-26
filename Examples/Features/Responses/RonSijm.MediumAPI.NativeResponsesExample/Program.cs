using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using RonSijm.MediumAPI.NativeResponsesExample.Manual;

namespace RonSijm.MediumAPI.NativeResponsesExample;

public sealed class Program
{
    public static void Main(string[] args)
    {
        Build(args).Run();
    }

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<ResponseProbe>();
        configure?.Invoke(builder);
        var app = builder.Build();
        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
            app.MapMediumApiEndpoint<AliasResponseEndpoint>("/aliases/first/{id:int}").WithoutDefaultEndpointName();
            app.MapMediumApiEndpoint<AliasResponseEndpoint>("/aliases/second/{id:int}").WithoutDefaultEndpointName();
            app.MapGet("/fluent/{id:int}", Fluent).AddMediumApiFilter<MissingItemFilter>();
        }

        app.MapGet("/descriptions", (IApiDescriptionGroupCollectionProvider descriptions) => TypedResults.Ok(descriptions.GetMediumApiDescriptionSnapshot()));
        return app;
    }

    public static Ok<CatalogItem> Fluent(int id, ResponseProbe probe)
    {
        return probe.Reply(id, "fluent");
    }
}