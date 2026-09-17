using System.Diagnostics.CodeAnalysis;
using Comparison.MediumApi.Data;
using Comparison.MediumApi.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Endpoints.Pizzas;

internal sealed class CreatePizzaEndpoint : IEndpointAdapter
{
	private readonly PizzaStore store;

	public CreatePizzaEndpoint(PizzaStore store) => this.store = store;

	public HttpVerb Verb => HttpVerb.Post;

	[StringSyntax("Route")]
	public string Pattern => "pizzas";

	public Task<Created<Pizza>> HandleAsync(CreatePizzaRequest request, CancellationToken cancellationToken)
	{
		var pizza = store.Add(request.Name, request.Price);
		return Task.FromResult(TypedResults.Created($"/pizzas/{pizza.Id}", pizza));
	}
}
