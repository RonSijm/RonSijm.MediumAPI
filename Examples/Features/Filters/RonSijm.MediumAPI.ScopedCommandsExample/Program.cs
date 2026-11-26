using Microsoft.AspNetCore.DataProtection;

namespace RonSijm.MediumAPI.ScopedCommandsExample;

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
        builder.Services.AddScoped<CommandTrace>();
        builder.Services.AddScoped<ResourceCommands>();
        builder.Services.AddSingleton<CommandProbe>();
        builder.Services.AddSingleton(new OwnershipPause());
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        // Native Forbid results need a host-owned authentication scheme. This sample has no login engine.
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
            LegacyCommands.Map(app);
        }
        else
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        }

        return app;
    }
}