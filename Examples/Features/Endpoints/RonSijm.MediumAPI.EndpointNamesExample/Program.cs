using RonSijm.MediumAPI.EndpointNamesExample.Endpoints.Api;

namespace RonSijm.MediumAPI.EndpointNamesExample;

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
        app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());

        // These are additional mounts of the same endpoint class. Give each mount a
        // unique explicit name, or suppress only that mount's inferred fallback.
        app.MapMediumApiEndpoint<GetProductEndpoint>("/aliases/products/{id:int}").WithName("ProductAlias");
        app.MapMediumApiEndpoint<GetProductEndpoint>("/aliases/unnamed/{id:int}").WithoutDefaultEndpointName();
        app.MapMediumApiEndpoint<GetProductEndpoint>("/aliases/also-unnamed/{id:int}").WithoutDefaultEndpointName();
        app.MapMediumApiEndpoint<GetProductEndpoint>("/aliases/explicit-suppressed/{id:int}").WithoutDefaultEndpointName().WithName("ExplicitSuppressedAlias");
        // WithName is deliberately called after Map, including this handler's
        // attribute declaration. It remains the effective explicit name.
        app.MapMediumApiEndpoint<AttributeNamedEndpoint>("/aliases/attribute-override").WithName("HostOverride");
        return app;
    }
}

public sealed record ProductResponse(int Id, string Name);