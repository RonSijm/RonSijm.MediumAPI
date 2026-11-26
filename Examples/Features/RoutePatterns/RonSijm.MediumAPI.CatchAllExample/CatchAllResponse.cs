namespace RonSijm.MediumAPI.CatchAllExample;

public sealed record CatchAllResponse(string Kind, string? Path, int? TenantId = null);