using RonSijm.MediumAPI.EndpointIdentityExample.Endpoints.Manual;

namespace RonSijm.MediumAPI.EndpointIdentityExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<IdentityProbe>();
        configure?.Invoke(builder);
        var app = builder.Build();
        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
            app.MapGroup("/aliases").AddEndpointFilterFactory(IdentityFilters.Observe).MapMediumApiEndpoint<ManualIdentityEndpoint>("/manual/{value:int}").WithName("ManualIdentityAlias");
            MapNativeEcho(app);
        }

        return app;
    }

    public static void MapNativeEcho(IEndpointRouteBuilder endpoints) => endpoints.MapGet("/native/{value:int}", new NativeIdentityHandler().HandleAsync).WithName("NativeEcho").AddEndpointFilterFactory(IdentityFilters.Observe);
}