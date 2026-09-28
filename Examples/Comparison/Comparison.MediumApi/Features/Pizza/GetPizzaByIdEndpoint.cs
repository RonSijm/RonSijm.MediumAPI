using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Features.Pizza;

internal sealed class GetPizzaByIdEndpoint(PizzaStore store) : IGetEndpoint<int, Results<Ok<Pizza>, NotFound>>
{
	[StringSyntax("Route")]
	public string Pattern => "pizzas/{id:int}";

	public Task<Results<Ok<Pizza>, NotFound>> HandleAsync(int id, CancellationToken cancellationToken)
	{
		var pizza = store.GetById(id);
		return Task.FromResult<Results<Ok<Pizza>, NotFound>>(pizza is null ? TypedResults.NotFound() : TypedResults.Ok(pizza));
	}
}
