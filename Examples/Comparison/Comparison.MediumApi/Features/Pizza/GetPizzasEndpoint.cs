using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Features.Pizza;

internal sealed class GetPizzasEndpoint(PizzaStore store) : IGetEndpoint<Ok<IEnumerable<Pizza>>>
{
	[StringSyntax("Route")]
	public string Pattern => "pizzas";

	public Task<Ok<IEnumerable<Pizza>>> HandleAsync(CancellationToken cancellationToken)
		=> Task.FromResult(TypedResults.Ok(store.GetAll()));
}
