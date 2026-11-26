namespace RonSijm.MediumAPI.NativeFiltersExample;

public sealed class RequestTrace
{
    public Guid ScopeId { get; } = Guid.NewGuid();
    public List<string> Entries { get; } = [];
}

public sealed record NativeFilterResponse(int Value, Guid ScopeId, IReadOnlyList<string> Trace);

public sealed record NativeFilterError(string Code);

public sealed record FilterDeclaration(string Name);