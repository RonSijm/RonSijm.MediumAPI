namespace RonSijm.MediumAPI.NamespaceMetadataExample;

public static class SampleMetadata
{
    public const string MapTag = "Map fallback";
    public const string ParentTag = "Catalog";
    public const string ChildTag = "Child catalog";
    public const string DetailTag = "Product detail";
    public const string HiddenTag = "Hidden sample";
    public const string BaseTag = "Ignored CLR base";
    public const string DisabledTag = "Disabled sample";
    public const string HostTag = "Host override";
    public const string ParentSummary = "Catalog group summary";
    public const string ParentDescription = "Catalog group description.";
    public const string ChildSummary = "Child group summary";
    public const string ChildDescription = "Child group description.";
    public const string DetailSummary = "Product detail summary";
    public const string DetailDescription = "Product detail description.";
    public const string HostSummary = "Host callback summary";
    public const string HostDescription = "Host callback description.";
}

public sealed record MetadataResponse(string Value);

public sealed class MappingProbe
{
    public int DisabledConditions { get; set; }
    public int DisabledMaps { get; set; }
    public int DisabledChildConditions { get; set; }
    public int DisabledChildMaps { get; set; }
}