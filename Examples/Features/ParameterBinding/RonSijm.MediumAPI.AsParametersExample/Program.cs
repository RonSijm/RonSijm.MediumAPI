namespace RonSijm.MediumAPI.AsParametersExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton(new AggregateService(SampleMetadata.ServiceName));
        builder.Services.AddSingleton<AggregateProbe>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<BoundReadOptions>();
        builder.Services.AddSingleton<BoundReadProbe>();
        builder.Services.AddSingleton<BoundDetailService>();
        builder.Services.AddSingleton<BoundVerificationService>();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
        configure?.Invoke(builder);
        var app = builder.Build();
        if (mapEndpoints && builder.Configuration.GetValue<bool>("Sample:BoundRouteReads")) { BoundReadScenario.Map(app); }
        else if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        }

        return app;
    }
}