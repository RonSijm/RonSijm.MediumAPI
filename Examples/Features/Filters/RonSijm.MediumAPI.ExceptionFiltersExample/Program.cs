namespace RonSijm.MediumAPI.ExceptionFiltersExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args ?? [],
            ApplicationName = typeof(Program).Assembly.FullName,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<LegacyProductCatalog>();
        builder.Services.AddScoped<RequestTrace>();
        configure?.Invoke(builder);
        var app = builder.Build();
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        return app;
    }
}