using RonSijm.MediumAPI.AcceptsExample.Endpoints.Manual;

namespace RonSijm.MediumAPI.AcceptsExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<RequestProbe>();
        configure?.Invoke(builder);
        var app = builder.Build();
        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
            app.MapMediumApiEndpoint<LaterOverrideEndpoint>("/requests/later").Accepts<PublishRequest>("text/plain");
            app.MapMediumApiEndpoint<RawAliasEndpoint>("/aliases/first").WithName("FirstRawAlias");
            app.MapMediumApiEndpoint<RawAliasEndpoint>("/aliases/second").WithName("SecondRawAlias");
        }

        return app;
    }
}