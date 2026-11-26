namespace RonSijm.MediumAPI.EndpointCorsExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true, bool useCors = true, string? middlewarePolicyName = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<CorsProbe>();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy.WithOrigins(SamplePolicies.DefaultOrigin).WithMethods("GET").WithHeaders(SamplePolicies.DefaultRequestHeader));
            options.AddPolicy(SamplePolicies.Browser, policy => policy.WithOrigins(SamplePolicies.BrowserOrigin, SamplePolicies.OtherBrowserOrigin).WithMethods("GET", "POST").WithHeaders(SamplePolicies.RequestHeader).WithExposedHeaders(SamplePolicies.ResponseHeader).AllowCredentials().SetPreflightMaxAge(TimeSpan.FromSeconds(600)));
            options.AddPolicy(SamplePolicies.Override, policy => policy.WithOrigins(SamplePolicies.OverrideOrigin).AllowAnyHeader().AllowAnyMethod());
        });
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseRouting();
        if (useCors)
        {
            if (middlewarePolicyName is null)
            {
                app.UseCors();
            }
            else
            {
                app.UseCors(middlewarePolicyName);
            }
        }

        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        }

        return app;
    }
}