namespace RonSijm.MediumAPI.RequestBodyLimitsExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<BodyLimitProbe>();
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = BodyLimits.Server);
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseRouting();
        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        }

        return app;
    }
}