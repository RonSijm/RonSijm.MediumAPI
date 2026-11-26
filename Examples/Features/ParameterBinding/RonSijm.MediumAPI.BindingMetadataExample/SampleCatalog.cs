namespace RonSijm.MediumAPI.BindingMetadataExample;

public sealed class SampleCatalog
{
    private int reads;
    public int Reads => Volatile.Read(ref reads);

    public ProductResponse Read(long id, int? page, string? revision)
    {
        Interlocked.Increment(ref reads);
        return new ProductResponse(id, $"sample-{id}", page, revision);
    }
}