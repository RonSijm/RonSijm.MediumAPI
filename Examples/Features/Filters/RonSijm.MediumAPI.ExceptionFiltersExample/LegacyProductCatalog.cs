namespace RonSijm.MediumAPI.ExceptionFiltersExample;

// This intentionally models an existing exception contract. New application
// branches should prefer explicit outcomes for routine missing/conflict cases.
public sealed class LegacyProductCatalog
{
    public Task<ProductResponse> LoadAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return id switch
        {
            1 => Task.FromResult(new ProductResponse(1, "Sample notebook")),
            2 => Task.FromException<ProductResponse>(new ProductMissingException()),
            3 => Task.FromException<ProductResponse>(new ProductConflictException()),
            _ => Task.FromException<ProductResponse>(new InvalidOperationException("Synthetic unexpected catalog failure."))
        };
    }
}

public sealed record ProductResponse(int Id, string Name);

public sealed class ProductMissingException : Exception { }

public sealed class ProductConflictException : Exception { }