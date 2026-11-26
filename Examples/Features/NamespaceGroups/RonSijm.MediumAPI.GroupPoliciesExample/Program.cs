using Microsoft.AspNetCore.Authentication.Cookies;

namespace RonSijm.MediumAPI.GroupPoliciesExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            // This sample has API responses rather than interactive login redirects.
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        builder.Services.AddAuthorization(options => options.AddPolicy(ExamplePolicies.Readers, policy => policy.RequireAuthenticatedUser().RequireClaim("scope", "sample.read")));
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(ExamplePolicies.Browser, policy => policy.WithOrigins(ExamplePolicies.AllowedOrigin).AllowAnyHeader().AllowAnyMethod());
            options.AddDefaultPolicy(policy => policy.WithOrigins(ExamplePolicies.AllowedOrigin).AllowAnyHeader().AllowAnyMethod());
        });
        configure?.Invoke(builder);

        var app = builder.Build();
        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        return app;
    }
}