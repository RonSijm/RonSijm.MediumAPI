using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace RonSijm.MediumAPI.ApiDescriptionsExample;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        await using var app = Build(args);
        await app.StartAsync();
        // The sample caller chooses when to resolve API Explorer and capture. Inspect
        // this local in a debugger; no description document is served or logged.
        var descriptions = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().GetMediumApiDescriptionSnapshot();
        await app.WaitForShutdownAsync();
    }

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args ?? [],
            ApplicationName = typeof(Program).Assembly.FullName,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<SampleCatalog>();
        configure?.Invoke(builder);
        var app = builder.Build();
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        return app;
    }
}

public sealed record ProductDetailsResponse(long Id, string Name, string? Search, string? Revision);

public sealed record ProductCreateRequest(string Name);

public sealed record ProductCreatedResponse(long Id, string Name, string? RequestId);

public sealed record StatusResponse(string Status);