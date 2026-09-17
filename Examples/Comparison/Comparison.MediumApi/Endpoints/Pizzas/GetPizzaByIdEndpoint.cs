using System.Diagnostics.CodeAnalysis;
using Comparison.MediumApi.Data;
using Comparison.MediumApi.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Endpoints.Pizzas;

internal sealed class GetPizzaByIdEndpoint : IEndpointAdapter
{
	private readonly PizzaStore store;

	public GetPizzaByIdEndpoint(PizzaStore store) => this.store = store;

	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "pizzas/{id:int}";

	public Task<Results<Ok<Pizza>, NotFound>> HandleAsync(int id, CancellationToken cancellationToken)
	{
		var pizza = store.GetById(id);
		return Task.FromResult<Results<Ok<Pizza>, NotFound>>(pizza is null ? TypedResults.NotFound() : TypedResults.Ok(pizza));
	}
}
