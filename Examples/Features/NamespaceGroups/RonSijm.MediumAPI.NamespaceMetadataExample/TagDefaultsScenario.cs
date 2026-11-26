namespace RonSijm.MediumAPI.NamespaceMetadataExample;

public static class TagDefaults
{
    public const string Shared = "Catalog reads";
    public const string Child = "Catalog history";
    public const string Different = "Catalog diagnostics";
    public const string Alias = "Public catalog";
}

public static class TagDefaultsScenario
{
    public static void Map(WebApplication app)
    {
        app.MapMediumApiNamespace<TagDefaultRoutes.TagDefaultsGroupDefinition>();
        // A standalone mapping does not import its endpoint's automatic namespace owner.
        app.MapMediumApiEndpoint<TagDefaultRoutes.GetTagDefaultStatusEndpoint>("/tag-defaults-alias").WithoutDefaultEndpointName().WithTags(TagDefaults.Alias);
    }
}