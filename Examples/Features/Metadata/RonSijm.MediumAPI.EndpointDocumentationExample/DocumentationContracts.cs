namespace RonSijm.MediumAPI.EndpointDocumentationExample;

public static class DocumentationGroups
{
    public const string Public = "public";
    public const string Admin = "admin";
    public const string Unmatched = "unmatched";
}

public static class DocumentationTags
{
    public const string Group = "Group defaults";
    public const string Catalog = "Catalog";
    public const string Read = "Read";
    public const string Admin = "Administration";
    public const string Method = "Method override";
    public const string Configured = "Configured override";
    public const string Hidden = "Hidden";
    public const string Shared = "Shared";
    public const string Unmatched = "Unmatched";
}

public sealed record DocumentationResponse(string Kind);

public sealed class DocumentationProbe
{
    private int calls;
    public int Calls => Volatile.Read(ref calls);

    public DocumentationResponse Record(string kind)
    {
        Interlocked.Increment(ref calls);
        return new DocumentationResponse(kind);
    }
}