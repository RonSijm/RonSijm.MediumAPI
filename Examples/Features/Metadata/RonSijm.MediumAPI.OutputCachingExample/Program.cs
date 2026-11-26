namespace RonSijm.MediumAPI.OutputCachingExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true, bool useOutputCache = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<CacheProbe>();
        builder.Services.AddOutputCache(options => options.AddPolicy(CachePolicies.Catalog, policy => policy.Expire(TimeSpan.FromSeconds(600))));
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseRouting();
        if (useOutputCache)
        {
            app.UseOutputCache();
        }

        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        }

        return app;
    }
}