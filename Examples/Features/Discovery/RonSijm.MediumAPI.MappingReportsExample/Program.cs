using RonSijm.MediumAPI.MappingReportsExample.Features.Conditional.Child;

namespace RonSijm.MediumAPI.MappingReportsExample;

public sealed class Program
{
    public static MediumApiDiscoveryProfile EndpointDiscovery { get; } = MediumApiDiscoveryProfile.Create(options => options.FromAssemblyContaining<Program>());

    public static void Main(string[] args) => Build(out _, args).Run();

    public static WebApplication Build(out MediumApiMappingReport report, string[]? args = null, bool conditionalEnabled = false, Action<WebApplicationBuilder>? configure = null)
    {
        var app = Create(args, conditionalEnabled, configure);
        app.MapMediumApiEndpoints(EndpointDiscovery, out report);
        // This mount happens outside the reported automatic operation. ManualOnly
        // therefore remains correct even though this alias is reachable on the host.
        app.MapMediumApiEndpoint<ManualNoteEndpoint>("/manual-alias").WithName("MappingReportsManualAlias");
        return app;
    }

    public static WebApplication Create(string[]? args = null, bool conditionalEnabled = false, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args ?? [],
            ApplicationName = typeof(Program).Assembly.FullName,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Services.AddSingleton(new MappingProbe(conditionalEnabled));
        builder.Services.AddEndpointsApiExplorer();
        configure?.Invoke(builder);
        return builder.Build();
    }
}

public sealed class MappingProbe(bool conditionalEnabled)
{
    public bool ConditionalEnabled { get; } = conditionalEnabled;
    public int EnabledConditions { get; set; }
    public int NestedConditions { get; set; }
    public int ParentConditions { get; set; }
    public int ChildConditions { get; set; }
    public int LegacyCalls { get; set; }
    public int MetadataBuilds { get; set; }
    public int EndpointActivations { get; set; }
}

public sealed record MappingValue(string Value);