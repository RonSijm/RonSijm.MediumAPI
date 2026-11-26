namespace RonSijm.MediumAPI.BindingMetadataExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args ?? [],
            ApplicationName = typeof(Program).Assembly.FullName,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<SampleCatalog>();
        builder.Services.AddSingleton<HeaderBindingProbe>();
        builder.Services.AddSingleton<HeaderCommandService>();
        configure?.Invoke(builder);
        if (builder.Configuration.GetValue<bool>("Sample:NamedHeaderBindings")) { builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false); }

        var app = builder.Build();
        if (mapEndpoints && builder.Configuration.GetValue<bool>("Sample:NamedHeaderBindings")) { NamedHeaderScenario.Map(app); }
        else if (mapEndpoints) { app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>()); }

        return app;
    }
}

public sealed record ProductResponse(long Id, string Name, int? Page, string? Revision);

public sealed record ProductCreateRequest(string Name);

public sealed record ProductCreatedResponse(long Id, string Name, string? Revision);

public sealed record SelectionResponse(string Value, string Source);

public sealed record StatusResponse(string Status);