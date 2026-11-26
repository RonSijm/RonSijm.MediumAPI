namespace RonSijm.MediumAPI.OpenApiMigrationExample;

public enum MappingStyle
{
    Native,
    MediumApi
}

public sealed record ProductResponse(long Id, string Name, string? Description);

public sealed record CreateProductRequest(string Name, string? Description);

public sealed record ValidationResponse(string Code);

public sealed record InternalStatusResponse(string Status);

internal static class SampleMetadata
{
    internal const string DocumentName = "v1";
    internal const string CatalogTag = "Catalog";
    internal const string TraceHeader = "X-Sample-Trace";
    internal const string GetSummary = "Get one sample product";
    internal const string GetDescription = "Reads a synthetic product while retaining route, query and header binding descriptions.";
    internal const string CreateSummary = "Create one sample product response";
    internal const string CreateDescription = "Returns a fixed synthetic identity; the example does not persist products.";
}