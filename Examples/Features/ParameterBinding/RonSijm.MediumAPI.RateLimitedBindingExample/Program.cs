using Microsoft.AspNetCore.RateLimiting;

namespace RonSijm.MediumAPI.RateLimitedBindingExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true, bool useRateLimiter = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<BindingProbe>();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter(SamplePolicies.Named, limiter =>
            {
                limiter.PermitLimit = 1;
                limiter.Window = TimeSpan.FromDays(1);
                limiter.AutoReplenishment = false;
                limiter.QueueLimit = 0;
            });
            options.OnRejected = (context, cancellationToken) => SamplePolicies.Reject(context, SamplePolicies.HostRejection);
        });
        configure?.Invoke(builder);
        var app = builder.Build();
        app.UseRouting();
        if (useRateLimiter)
        {
            app.UseRateLimiter();
        }

        if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        }

        return app;
    }
}