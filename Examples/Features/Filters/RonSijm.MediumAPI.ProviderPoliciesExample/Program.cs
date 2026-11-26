using Microsoft.AspNetCore.DataProtection;

namespace RonSijm.MediumAPI.ProviderPoliciesExample;

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
        builder.Services.AddScoped<PolicyTrace>();
        builder.Services.AddScoped<ProviderOperations>();
        builder.Services.AddSingleton<ProviderProbe>();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAuthentication("Sample").AddCookie("Sample", options =>
        {
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        if (legacy || builder.Configuration.GetValue<bool>("Sample:Legacy"))
        {
            LegacyProviders.Map(app);
        }
        else
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        }

        return app;
    }
}