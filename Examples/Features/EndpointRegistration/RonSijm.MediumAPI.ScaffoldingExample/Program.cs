namespace RonSijm.MediumAPI.ScaffoldingExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        builder.Services.AddMediumApiEndpoints();
        configure?.Invoke(builder);

        var app = builder.Build();
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        return app;
    }
}