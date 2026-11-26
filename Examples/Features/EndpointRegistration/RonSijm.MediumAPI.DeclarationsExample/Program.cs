namespace RonSijm.MediumAPI.DeclarationsExample;

public sealed class Program
{
    public static MediumApiDiscoveryProfile EndpointDiscovery { get; } = MediumApiDiscoveryProfile.Create(options => options.FromAssemblyContaining<Program>());

    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, DeclarationOptions? options = null, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args ?? [],
            ApplicationName = typeof(Program).Assembly.FullName,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton(options ?? new DeclarationOptions());
        configure?.Invoke(builder);
        var app = builder.Build();
        app.MapMediumApiEndpoints(EndpointDiscovery);
        return app;
    }
}

public sealed record DeclarationOptions(bool EnableApi = true, bool EnableOptional = true);

public sealed record DeclarationResponse(string Shape);