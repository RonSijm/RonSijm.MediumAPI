using RonSijm.MediumAPI.ResponseFallbackExample.Endpoints.Manual;

namespace RonSijm.MediumAPI.ResponseFallbackExample;

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
        builder.Services.AddSingleton<FallbackProbe>();
        configure?.Invoke(builder);
        var app = builder.Build();
        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
            app.MapMediumApiEndpoint<AliasFallbackEndpoint>("/aliases/first").WithName("FirstFallbackAlias");
            app.MapMediumApiEndpoint<AliasFallbackEndpoint>("/aliases/second").WithName("SecondFallbackAlias");
            var later = app.MapMediumApiNamespace<LaterFinalGroupDefinition>();
            ((IEndpointConventionBuilder)later).Finally(endpoint => endpoint.Metadata.Add(new ProducesResponseTypeMetadata(404, typeof(LaterFailure), ["text/plain"])));
            later.MapMediumApiEndpoint<LaterFinalEndpoint>();
        }

        return app;
    }
}