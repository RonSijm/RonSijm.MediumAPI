namespace RonSijm.MediumAPI.StructBindingExample;

public sealed class AggregateProbe
{
    private int calls;
    public int Calls => Volatile.Read(ref calls);
    public void RecordHandler() => Interlocked.Increment(ref calls);
}