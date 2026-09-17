using System.Diagnostics.CodeAnalysis;
using Comparison.MediumApi.Data;
using Comparison.MediumApi.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Endpoints.Pizzas;

internal sealed class GetPizzasEndpoint : IEndpointAdapter
{
	private readonly PizzaStore store;

	public GetPizzasEndpoint(PizzaStore store) => this.store = store;

	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "pizzas";

	public Task<Ok<IEnumerable<Pizza>>> HandleAsync(CancellationToken cancellationToken)
		=> Task.FromResult(TypedResults.Ok(store.GetAll()));
}
