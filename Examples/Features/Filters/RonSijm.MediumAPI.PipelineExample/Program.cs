using RonSijm.MediumAPI.PipelineExample.Endpoints;

namespace RonSijm.MediumAPI.PipelineExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        builder.Services.AddSingleton<LifetimeProbe>();
        builder.Services.AddScoped<RequestDependency>();
        builder.Services.AddEndpointsApiExplorer();
        configure?.Invoke(builder);
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            try { await next(context); }
            catch (ExampleFailureException) { context.Response.StatusCode = StatusCodes.Status500InternalServerError; }
        });
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        app.MapGroup("/aliases/{tenantId:int}").AddEndpointFilterFactory(PipelineFilters.Increment)
            .MapMediumApiEndpoint<ManualEndpoint>("/{value:int}").WithName("PipelineAlias")
            .AddEndpointFilterFactory(PipelineFilters.Triple);
        return app;
    }
}