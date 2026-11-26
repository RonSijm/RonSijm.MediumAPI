namespace RonSijm.MediumAPI.NamespaceMetadataExample;

public sealed class Program
{
    public static void Main(string[] args) => Build(args).Run();

    public static WebApplication Build(string[]? args = null, Action<WebApplicationBuilder>? configure = null, bool mapEndpoints = true)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args ?? [], ApplicationName = typeof(Program).Assembly.FullName, ContentRootPath = AppContext.BaseDirectory });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSingleton<MappingProbe>();
        builder.Services.AddSingleton<DeclarativeMappingProbe>();
        builder.Services.AddSingleton<EndpointPatternProbe>();
        configure?.Invoke(builder);
        if (builder.Configuration.GetValue<bool>(EndpointPatternMetadata.Flag)) { builder.Services.AddAuthorization(options => options.AddPolicy(EndpointPatternMetadata.Policy, policy => policy.RequireAssertion(_ => true))); }

        var app = builder.Build();
        if (builder.Configuration.GetValue<bool>(EndpointPatternMetadata.Flag)) { app.UseAuthorization(); }

        if (mapEndpoints && builder.Configuration.GetValue<bool>(EndpointPatternMetadata.Flag)) { EndpointPatternScenario.Map(app); }
        else if (mapEndpoints && builder.Configuration.GetValue<bool>("Sample:TagDefaults")) { TagDefaultsScenario.Map(app); }
        else if (mapEndpoints && builder.Configuration.GetValue<bool>("Sample:DeclarativeGroups")) { DeclarativeGroupsScenario.Map(app); }
        else if (mapEndpoints)
        {
            app.MapMediumApiEndpoints(options => options.FromAssemblyContaining<Program>());
        }

        return app;
    }
}