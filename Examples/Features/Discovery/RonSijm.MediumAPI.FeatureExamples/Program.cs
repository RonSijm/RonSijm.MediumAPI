using RonSijm.MediumAPI.FeatureExamples.Endpoints.Api;

namespace RonSijm.MediumAPI.FeatureExamples;

public sealed class Program
{
    // Assembly selection can be reused for composition and declaration inspection.
    // Selecting the same assembly twice does not register its endpoints twice.
    public static MediumApiDiscoveryProfile EndpointDiscovery { get; } = MediumApiDiscoveryProfile.Create(options => options.FromAssemblyContaining<Program>().FromAssembly(typeof(Program).Assembly));

    public static void Main(string[] args)
    {
        var app = Build(args);
        app.Run();
    }

    public static WebApplication Build(string[]? args = null, string? environment = null, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args ?? [],
            ApplicationName = typeof(Program).Assembly.FullName,
            EnvironmentName = environment,
            ContentRootPath = AppContext.BaseDirectory
        });
        var defaults = new Dictionary<string, string?>();
        if (builder.Configuration["Features:Development"] is null)
        {
            defaults["Features:Development"] = "false";
        }

        if (builder.Configuration["Features:Child"] is null)
        {
            defaults["Features:Child"] = "true";
        }

        builder.Configuration.AddInMemoryCollection(defaults);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton<FeatureMappingProbe>();
        configure?.Invoke(builder);
        var app = builder.Build();
        app.MapMediumApiEndpoints(EndpointDiscovery);
        app.MapMediumApiEndpoint<ManualAliasEndpoint>("/exports/current").WithName("FeatureManualAlias");
        return app;
    }
}

public sealed class FeatureMappingProbe
{
    public int DevelopmentConditions { get; set; }
    public int DevelopmentMaps { get; set; }
    public int ChildConditions { get; set; }
    public int ChildMaps { get; set; }
    public int EndpointActivations { get; set; }
}