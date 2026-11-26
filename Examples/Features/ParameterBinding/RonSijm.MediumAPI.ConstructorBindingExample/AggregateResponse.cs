namespace RonSijm.MediumAPI.ConstructorBindingExample;

public sealed record AggregateResponse(string Kind, long ItemId, long? CustomerId, int? Page, string? Label, string ServiceName, bool OriginalAggregate, int ArgumentCount);