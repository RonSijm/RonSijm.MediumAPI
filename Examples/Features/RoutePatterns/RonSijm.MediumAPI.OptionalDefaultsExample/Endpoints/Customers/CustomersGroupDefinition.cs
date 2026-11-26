using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.OptionalDefaultsExample.Endpoints.Customers;

internal sealed class CustomersGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/customers/{customerId:long?}");
}

internal sealed class GetOptionalCustomerEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "";

    public Ok<OptionalValueResponse> HandleAsync([FromRoute(Name = "customerId")] long? accountId) => TypedResults.Ok(new OptionalValueResponse("customer", accountId));
}