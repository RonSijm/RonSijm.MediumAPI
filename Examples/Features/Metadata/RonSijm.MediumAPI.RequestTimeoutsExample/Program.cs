namespace RonSijm.MediumAPI.RequestTimeoutsExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true, bool useRequestTimeouts = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<TimeoutProbe>();
        builder.Services.AddRequestTimeouts(options =>
        {
            options.AddPolicy(TimeoutPolicies.Group, TimeoutPolicies.CreatePolicy(TimeoutPolicies.Group, TimeSpan.FromSeconds(30)));
            options.AddPolicy(TimeoutPolicies.Class, TimeoutPolicies.CreatePolicy(TimeoutPolicies.Class, TimeSpan.FromSeconds(30)));
            options.AddPolicy(TimeoutPolicies.Method, TimeoutPolicies.CreatePolicy(TimeoutPolicies.Method, TimeSpan.FromSeconds(30)));
        });
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseRouting();
        if (useRequestTimeouts)
        {
            app.UseRequestTimeouts();
        }

        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        }

        return app;
    }
}