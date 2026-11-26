namespace RonSijm.MediumAPI.RateLimitingExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true, bool useRateLimiter = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<RateLimitProbe>();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            RateLimitPolicies.AddFixedWindow(options, RateLimitPolicies.Map, 8);
            RateLimitPolicies.AddFixedWindow(options, RateLimitPolicies.Parent, 1);
            RateLimitPolicies.AddFixedWindow(options, RateLimitPolicies.Child, 2);
            RateLimitPolicies.AddFixedWindow(options, RateLimitPolicies.Endpoint, 3);
            RateLimitPolicies.AddFixedWindow(options, RateLimitPolicies.Host, 4);
            options.OnRejected = (context, cancellationToken) =>
            {
                context.HttpContext.RequestServices.GetRequiredService<RateLimitProbe>().RecordRejection();
                context.HttpContext.Response.Headers[RateLimitPolicies.RejectionHeader] = RateLimitPolicies.RejectionValue;
                return ValueTask.CompletedTask;
            };
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