namespace RonSijm.MediumAPI.RouteBindingExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args ?? [],
            ApplicationName = typeof(Program).Assembly.FullName,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Services.AddEndpointsApiExplorer();
        configure?.Invoke(builder);
        var app = builder.Build();
        // This host-owned prefix is intentionally outside namespace declarations.
        // The tenant advisory in GetHostTenantEndpoint therefore is not a route error.
        app.MapGroup("/tenants/{tenant}").MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        return app;
    }
}

public sealed record RouteBindingResponse(long CustomerId, int? ItemId, string? Tenant);