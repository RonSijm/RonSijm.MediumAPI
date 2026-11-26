namespace RonSijm.MediumAPI.ApiDescriptionsExample;

// Synthetic service used to distinguish description capture from handler dispatch.
public sealed class SampleCatalog
{
    private int reads;
    public int Reads => Volatile.Read(ref reads);

    public ProductDetailsResponse Read(long id, string? search, string? revision)
    {
        Interlocked.Increment(ref reads);
        return new ProductDetailsResponse(id, $"sample-{id}", search, revision);
    }
}