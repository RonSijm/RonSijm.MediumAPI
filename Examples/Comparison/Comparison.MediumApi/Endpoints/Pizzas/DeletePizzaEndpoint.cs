using System.Diagnostics.CodeAnalysis;
using Comparison.MediumApi.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Endpoints.Pizzas;

internal sealed class DeletePizzaEndpoint : IEndpointAdapter
{
	private readonly PizzaStore store;

	public DeletePizzaEndpoint(PizzaStore store) => this.store = store;

	public HttpVerb Verb => HttpVerb.Delete;

	[StringSyntax("Route")]
	public string Pattern => "pizzas/{id:int}";

	public Task<Results<NoContent, NotFound>> HandleAsync(int id, CancellationToken cancellationToken)
		=> Task.FromResult<Results<NoContent, NotFound>>(store.Delete(id) ? TypedResults.NoContent() : TypedResults.NotFound());
}
