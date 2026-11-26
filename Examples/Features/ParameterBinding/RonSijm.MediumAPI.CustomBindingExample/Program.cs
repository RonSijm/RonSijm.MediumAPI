namespace RonSijm.MediumAPI.CustomBindingExample;

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
        configure?.Invoke(builder);
        var app = builder.Build();
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        return app;
    }
}

public sealed record BindingResponse(string Source, string? Value, string? ParameterName = null, string? RouteValue = null);