namespace RonSijm.MediumAPI.DocumentGroupsExample;

public sealed class DocumentProbe
{
    private int calls;
    public int Calls => Volatile.Read(ref calls);

    public DocumentResponse Record(string kind)
    {
        Interlocked.Increment(ref calls);
        return new DocumentResponse(kind);
    }
}