using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using RonSijm.MediumAPI.AntiforgeryOptOutExample.Endpoints;

namespace RonSijm.MediumAPI.AntiforgeryOptOutExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<FormProbe>();
        // This isolated sample intentionally uses keys that disappear when its host stops.
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = SampleAntiforgery.HeaderName;
            options.Cookie.Name = SampleAntiforgery.CookieName;
            options.FormFieldName = SampleAntiforgery.FormFieldName;
        });
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseRouting();
        app.UseAntiforgery();
        if (mapEndpoints)
        {
            Map(app);
        }

        return app;
    }

    public static void Map(WebApplication app)
    {
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        app.MapMediumApiEndpoint<PostClassOptOutFormEndpoint>("/aliases/class-form").WithoutDefaultEndpointName();
        var requiredAgain = app.MapMediumApiEndpoint<PostClassOptOutFormEndpoint>("/aliases/required-again").WithoutDefaultEndpointName();
        // A later native endpoint final convention retains its normal priority.
        ((IEndpointConventionBuilder)requiredAgain).Finally(endpoint => endpoint.Metadata.Add(new RequireAntiforgeryTokenAttribute()));
    }
}