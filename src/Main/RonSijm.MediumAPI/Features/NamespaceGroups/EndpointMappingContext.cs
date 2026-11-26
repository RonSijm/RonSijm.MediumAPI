using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RonSijm.MediumAPI;

/// <summary>Host startup context for namespace conditions. Never contains request-scoped endpoint instances.</summary>
public sealed class EndpointMappingContext
{
    internal EndpointMappingContext(IServiceProvider services) => Services = services;

    public IServiceProvider Services { get; }
    public IConfiguration Configuration => Services.GetRequiredService<IConfiguration>();
    public IHostEnvironment Environment => Services.GetRequiredService<IHostEnvironment>();
}