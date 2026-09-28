using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Features.Pizza;

internal sealed class DeletePizzaEndpoint(PizzaStore store) : IDeleteEndpoint<int, Results<NoContent, NotFound>>
{
	[StringSyntax("Route")]
	public string Pattern => "pizzas/{id:int}";

	public Task<Results<NoContent, NotFound>> HandleAsync(int id, CancellationToken cancellationToken)
		=> Task.FromResult<Results<NoContent, NotFound>>(store.Delete(id) ? TypedResults.NoContent() : TypedResults.NotFound());
}
