using Microsoft.AspNetCore.Authentication.Cookies;

namespace RonSijm.MediumAPI.ResponseHeadersExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<DerivedHeaderProbe>();
        builder.Services.AddSingleton<HeaderSetProbe>();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        });
        builder.Services.AddAuthorization();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        if (builder.Configuration.GetValue<bool>("Sample:HeaderSets")) { HeaderSetsSample.Map(app); }
        else if (builder.Configuration.GetValue<bool>("Sample:DerivedHeaders")) { DerivedHeaderSample.Map(app); }
        else
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
            app.MapGet("/fluent", () => TypedResults.Ok(new HeaderItem(1, "fluent"))).WithResponseHeader("CacheControl", "no-store").WithResponseHeader("X-Fluent", "endpoint").WithName("NativeHeaders");
            app.MapGroup("/fluent-group").WithResponseHeader("X-Fluent", "group").MapGet("/value", () => TypedResults.Ok("group"));
        }

        return app;
    }
}