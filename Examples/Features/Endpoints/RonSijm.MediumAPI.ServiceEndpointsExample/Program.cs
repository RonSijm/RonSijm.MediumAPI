using RonSijm.MediumAPI.ServiceEndpointsExample.Endpoints.Api;
using RonSijm.MediumAPI.ServiceEndpointsExample.Endpoints.Manual;

namespace RonSijm.MediumAPI.ServiceEndpointsExample;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var app = Build(args);
        using var callerOwned = app.Services.GetRequiredService<GetExternalServiceEndpoint>();
        app.Run();
    }

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        var probe = new ServiceProbe();
        builder.Services.AddSingleton(probe);
        builder.Services.AddScoped<RequestDependency>();
        builder.Services.AddScoped<GetScopedServiceEndpoint>();
        builder.Services.AddScoped(provider => new GetFactoryServiceEndpoint(provider.GetRequiredService<ServiceProbe>(), provider.GetRequiredService<RequestDependency>(), "configured-factory"));
        builder.Services.AddTransient<GetTransientServiceEndpoint>();
        builder.Services.AddSingleton<GetSingletonServiceEndpoint>();
        builder.Services.AddSingleton(new GetExternalServiceEndpoint(probe));
        builder.Services.AddSingleton(provider => new GetDefaultServiceEndpoint(provider.GetRequiredService<ServiceProbe>(), "registered-default"));
        builder.Services.AddScoped<GetDeferredServiceEndpoint>();
        builder.Services.AddScoped<ManualServiceEndpoint>();
        builder.Services.AddEndpointsApiExplorer();
        configure?.Invoke(builder);
        var app = builder.Build();
        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
            app.MapGroup("/aliases").MapMediumApiEndpoint<ManualServiceEndpoint>("/manual/{value:int}").WithName("ManualServiceAlias");
        }

        return app;
    }
}