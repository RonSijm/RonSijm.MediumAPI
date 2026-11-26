using System.Collections.Concurrent;

namespace RonSijm.MediumAPI.OutputCachingExample;

public static class CachePolicies
{
    public const string Catalog = "sample-catalog-policy";
    public const string CatalogTag = "sample-catalog";
    public const string LanguageHeader = "X-Sample-Language";
    public const string CookieName = "mediumapi-sample";
}

public sealed record CacheResponse(string Kind, int Sequence, string? Query = null, string? Header = null);

/// <summary>Counts handler dispatches for the sample; response storage belongs to ASP.NET Core.</summary>
public sealed class CacheProbe
{
    private readonly ConcurrentDictionary<string, int> _calls = new(StringComparer.Ordinal);

    public CacheResponse Record(string kind, string? query = null, string? header = null) => new(kind, _calls.AddOrUpdate(kind, 1, static (_, count) => count + 1), query, header);

    public int GetCount(string kind) => _calls.TryGetValue(kind, out var count) ? count : 0;
}