namespace RonSijm.MediumAPI.GroupPoliciesExample.Endpoints.Api.Reports;

public sealed class ReportsGroupDefinition : INamespaceEndpointGroup
{
    // The parent route builder supplies the authorization and CORS policies.
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/reports");
}