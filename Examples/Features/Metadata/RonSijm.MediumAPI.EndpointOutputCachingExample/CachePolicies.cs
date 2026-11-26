using System.Collections.Concurrent;

namespace RonSijm.MediumAPI.EndpointOutputCachingExample;

public static class CachePolicies
{
    public const string Catalog = "sample-endpoint-catalog-policy";
    public const string CatalogTag = "sample-endpoint-catalog";
    public const string LanguageHeader = "X-Sample-Language";
    public const string CookieName = "mediumapi-output-cache-sample";
}

public sealed record CacheResponse(string Kind, int Sequence, string? Query = null, string? Header = null);

/// <summary>Counts handler calls only; ASP.NET Core owns response storage, keys and cache policies.</summary>
public sealed class CacheProbe
{
    private readonly ConcurrentDictionary<string, int> calls = new(StringComparer.Ordinal);

    public CacheResponse Record(string kind, string? query = null, string? header = null) => new(kind, calls.AddOrUpdate(kind, 1, static (_, count) => count + 1), query, header);

    public int GetCount(string kind) => calls.TryGetValue(kind, out var count) ? count : 0;
}