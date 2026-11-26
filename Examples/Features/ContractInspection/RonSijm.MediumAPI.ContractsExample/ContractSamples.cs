namespace RonSijm.MediumAPI.ContractsExample;

/// <summary>Explicit caller-side inspection only; the sample host does not invoke these helpers or expose a diagnostics route.</summary>
public static class ContractSamples
{
    public static string CaptureJson(IEndpointRouteBuilder endpoints) => endpoints.GetMediumApiContractSnapshot().ToJson();

    public static bool HaveEquivalentContracts(IEndpointRouteBuilder expected, IEndpointRouteBuilder actual)
    {
        var comparison = MediumApiContractComparison.Compare(expected.GetMediumApiContractSnapshot(), actual.GetMediumApiContractSnapshot());
        return comparison.IsEquivalent;
    }

    public static bool HaveEquivalentOpenApi(string expectedJson, string actualJson) => MediumApiContractComparison.CompareOpenApi(expectedJson, actualJson).IsEquivalent;
}