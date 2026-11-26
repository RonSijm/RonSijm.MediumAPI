using Microsoft.AspNetCore.DataProtection;

namespace RonSijm.MediumAPI.AntiforgeryExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true, bool useAntiforgery = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<FormProbe>();
        builder.Services.AddSingleton<ManualFormProbe>();
        // An isolated sample host: keys are kept in memory and disappear when it stops.
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = SampleAntiforgery.HeaderName;
            options.FormFieldName = SampleAntiforgery.FormFieldName;
            options.Cookie.Name = SampleAntiforgery.CookieName;
        });
        configure?.Invoke(builder);

        var app = builder.Build();
        app.UseRouting();
        if (useAntiforgery)
        {
            app.UseAntiforgery();
        }

        if (mapEndpoints && builder.Configuration.GetValue<bool>("Sample:ManualForms")) { ManualFormsScenario.Map(app); }
        else if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        }

        return app;
    }
}