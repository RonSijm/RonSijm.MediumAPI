using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;

namespace RonSijm.MediumAPI.EndpointAuthorizationExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<AuthorizationProbe>();
        // Sample hosts have independent in-memory keys; production key management stays explicit.
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAuthentication(SamplePolicies.PrimaryScheme)
            .AddCookie(SamplePolicies.PrimaryScheme, options => ConfigureCookie(options, SamplePolicies.PrimaryCookie))
            .AddCookie(SamplePolicies.SecondaryScheme, options => ConfigureCookie(options, SamplePolicies.SecondaryCookie));
        builder.Services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy(SamplePolicies.Readers, policy => policy.RequireAuthenticatedUser().RequireClaim(SamplePolicies.PermissionClaim, "read"));
            options.AddPolicy(SamplePolicies.Writers, policy => policy.RequireAuthenticatedUser().RequireClaim(SamplePolicies.PermissionClaim, "write"));
            options.AddPolicy(SamplePolicies.GroupAccess, policy => policy.RequireAuthenticatedUser().RequireClaim("module", "catalog"));
            options.AddPolicy(SamplePolicies.Audit, policy => policy.RequireAuthenticatedUser().RequireClaim("audit", "allowed"));
        });
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        }

        return app;
    }

    private static void ConfigureCookie(CookieAuthenticationOptions options, string cookieName)
    {
        options.Cookie.Name = cookieName;
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
    }
}