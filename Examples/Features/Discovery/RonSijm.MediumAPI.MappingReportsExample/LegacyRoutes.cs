using System.Runtime.CompilerServices;

namespace RonSijm.MediumAPI.MappingReportsExample;

// A stable named callback models an older source registration. Its identity is
// reportable; the route contents are not inferred from that identity.
public static class LegacyRoutes
{
    [ModuleInitializer]
    internal static void Register() => EndpointRegistry.Register(Map);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.ServiceProvider.GetRequiredService<MappingProbe>().LegacyCalls++;
        endpoints.MapGet("/legacy-status", () => TypedResults.Ok(new MappingValue("legacy"))).WithName("MappingReportsLegacyStatus");
    }
}