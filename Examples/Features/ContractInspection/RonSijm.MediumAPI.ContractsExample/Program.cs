namespace RonSijm.MediumAPI.ContractsExample;

public sealed class Program
{
    public static MediumApiDiscoveryProfile EndpointDiscovery { get; } = MediumApiDiscoveryProfile.Create(options => options.FromAssemblyContaining<Program>());

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
        app.MapMediumApiEndpoints(EndpointDiscovery);
        return app;
    }
}