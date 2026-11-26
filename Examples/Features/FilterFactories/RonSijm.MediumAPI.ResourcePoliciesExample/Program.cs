using Microsoft.AspNetCore.DataProtection;

namespace RonSijm.MediumAPI.ResourcePoliciesExample;

public sealed class Program
{
    public static void Main(string[] args)
    {
        Build(args).Run();
    }

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool legacy = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<ResourceProbe>();
        builder.Services.AddScoped<ResourceTrace>();
        builder.Services.AddScoped<ResourceOperations>();
        builder.Services.AddScoped<OperationOutcomeService>();
        builder.Services.AddScoped<NamedArgumentOperations>();
        builder.Services.AddScoped<FactoryRequestScope>();
        builder.Services.AddSingleton<FactoryBuildLedger>();
        builder.Services.AddSingleton<DocumentPageProbe>();
        builder.Services.AddSingleton<DocumentPageFixture>();
        builder.Services.AddSingleton(services => new DocumentPageOptions { Enabled = services.GetRequiredService<IConfiguration>().GetValue("Sample:DocumentsEnabled", true) });
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAuthentication("Sample").AddCookie("Sample", options =>
        {
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            };
        });
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        if (legacy || builder.Configuration.GetValue<bool>("Sample:Legacy"))
        {
            LegacyResources.Map(app);
            LegacyOperationOutcomes.Map(app);
            LegacyNamedArgumentGuards.Map(app);
            LegacyGuardedStarts.Map(app);
            LegacyDocumentPages.Map(app);
        }
        else
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
            app.MapMediumApiEndpoint<ShiftedGuardedStartEndpoint>("/sample/start-alias").WithoutDefaultEndpointName();
            DocumentPageEntry.MapAlias(app);
        }

        return app;
    }
}