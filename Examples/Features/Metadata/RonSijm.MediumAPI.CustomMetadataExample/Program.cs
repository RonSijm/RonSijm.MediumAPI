using RonSijm.MediumAPI.CustomMetadataExample.Endpoints.Manual;

namespace RonSijm.MediumAPI.CustomMetadataExample;

public sealed class Program
{
    public static void Main(string[] args)
    {
        Build(args).Run();
    }

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<MetadataProbe>();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization(options => options.AddPolicy("SampleReaders", policy => policy.RequireAuthenticatedUser()));
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseRouting();
        app.UseAuthorization();
        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
            app.MapMediumApiEndpoint<AliasMetadataEndpoint>("/aliases/first").WithName("FirstMetadataAlias");
            app.MapMediumApiEndpoint<AliasMetadataEndpoint>("/aliases/second").WithName("SecondMetadataAlias");
        }

        return app;
    }
}