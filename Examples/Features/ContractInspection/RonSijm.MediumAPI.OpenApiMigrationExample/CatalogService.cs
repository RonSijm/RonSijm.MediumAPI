namespace RonSijm.MediumAPI.OpenApiMigrationExample;

/// <summary>Synthetic request state makes explicit inspection distinguishable from handler dispatch.</summary>
public sealed class CatalogService
{
    private int _reads;
    private int _writes;
    private long _lastId;
    private int _lastTake;
    private string? _lastSearch;
    private string? _lastTrace;

    public int Reads => Volatile.Read(ref _reads);
    public int Writes => Volatile.Read(ref _writes);
    public long LastId => Interlocked.Read(ref _lastId);
    public int LastTake => Volatile.Read(ref _lastTake);
    public string? LastSearch => Volatile.Read(ref _lastSearch);
    public string? LastTrace => Volatile.Read(ref _lastTrace);

    public ProductResponse? Find(long id, string? search, string? trace, int take)
    {
        Interlocked.Increment(ref _reads);
        Interlocked.Exchange(ref _lastId, id);
        Interlocked.Exchange(ref _lastTake, take);
        Volatile.Write(ref _lastSearch, search);
        Volatile.Write(ref _lastTrace, trace);
        return id == 17 ? new ProductResponse(17, "Sample product", "Synthetic catalog item") : null;
    }

    public ProductResponse Create(CreateProductRequest create)
    {
        Interlocked.Increment(ref _writes);
        return new ProductResponse(23, create.Name, create.Description);
    }
}