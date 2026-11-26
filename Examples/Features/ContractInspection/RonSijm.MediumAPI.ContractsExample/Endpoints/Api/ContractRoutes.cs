namespace RonSijm.MediumAPI.ContractsExample.Endpoints.Api;

internal static class ContractRoutes
{
    public const string Items = "/items";
    public const string Item = Items + "/{id:int}";
    public const string Status = "/" + nameof(Status);
}