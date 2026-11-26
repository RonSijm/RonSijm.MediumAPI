namespace RonSijm.MediumAPI.ConstructorBindingExample;

public sealed class AggregateService
{
    public AggregateService(string name) => Name = name;
    public string Name { get; }
}