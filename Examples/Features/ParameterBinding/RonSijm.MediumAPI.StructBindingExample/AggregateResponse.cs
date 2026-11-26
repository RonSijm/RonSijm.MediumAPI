namespace RonSijm.MediumAPI.StructBindingExample;

public sealed record AggregateResponse(string Kind, long ItemId, long? CustomerId, int? Page, string? Label, string ServiceName, bool FilterValueMatches, bool FilterServiceMatches, int ArgumentCount, bool ParameterlessConstructorUsed);