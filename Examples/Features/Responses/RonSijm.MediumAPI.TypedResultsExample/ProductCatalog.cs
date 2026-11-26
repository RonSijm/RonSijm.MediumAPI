namespace RonSijm.MediumAPI.TypedResultsExample;

/// <summary>A synthetic catalog with no database or external calls.</summary>
public sealed class ProductCatalog
{
    public Task<ProductResponse?> FindAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<ProductResponse?>(id == 1 ? new ProductResponse(1, "Sample notebook", 12.50m, "Plain pages") : null);
    }
}

public sealed record ProductResponse(int Id, string Name, decimal Price, string? Description);

public sealed record ServiceStatus(string Status);