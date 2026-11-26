using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI.AliasMountsExample.Endpoints.Owned;

namespace RonSijm.MediumAPI.AliasMountsExample;

public sealed class Program
{
    public static void Main(string[] args)
    {
        Build(args).Run();
    }

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory, EnvironmentName = "Testing" });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<AliasProbe>();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
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
        builder.Services.AddAuthorization(options => options.AddPolicy("AliasReaders", policy => policy.RequireAuthenticatedUser()));
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
            AliasConventions.Apply(app.MapMediumApiEndpointAliases<SharedAliasEndpoint>(["/root/first/{id:int}", "/root/second/{id:int}"]), "Root");
            var publicGroup = app.MapGroup("/public").WithResponseHeader("X-Group", "public");
            AliasConventions.Apply(publicGroup.MapMediumApiEndpointAliases<SharedAliasEndpoint>(["/first/{id:int}", "/second/{id:int}"]), "Public");
            var protectedGroup = app.MapGroup("/api").MapGroup("/private").RequireAuthorization("AliasReaders").WithMetadata(new ProducesResponseTypeMetadata(401)).WithResponseHeader("X-Group", "private");
            AliasConventions.Apply(protectedGroup.MapMediumApiEndpointAliases<SharedAliasEndpoint>(["/first/{id:int}", "/second/{id:int}"]), "Private");
            app.MapMediumApiEndpointAliases<NamedAliasEndpoint>(["/named/one"]);
            app.MapGet("/sign-in", (Func<HttpContext, Task<NoContent>>)SignIn).WithName("SignInAliasExample");
        }

        return app;
    }

    public static async Task<NoContent> SignIn(HttpContext context)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "sample")], CookieAuthenticationDefaults.AuthenticationScheme));
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return TypedResults.NoContent();
    }
}

public static class AliasConventions
{
    public static void Apply(RouteHandlerBuilder aliases, string scope)
    {
        aliases.WithTags("Alias mounts").WithResponseHeader("X-Aggregate", scope).AddMediumApiFilter<AggregateAliasFilter>();
        ((IEndpointConventionBuilder)aliases).Finally(builder =>
        {
            var path = ((RouteEndpointBuilder)builder).RoutePattern.RawText!;
            var suffix = path.Contains("/first/", StringComparison.Ordinal) ? "First" : "Second";
            builder.Metadata.Add(new EndpointNameMetadata(scope + suffix));
            builder.Metadata.Add(new RouteNameMetadata(scope + suffix));
            builder.Metadata.Add(new AliasFinalMetadata(scope));
        });
    }
}