using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Features.Pizza;

internal sealed class CreatePizzaEndpoint(PizzaStore store) : IPostEndpoint<CreatePizzaRequest, Created<Pizza>>
{
	[StringSyntax("Route")]
	public string Pattern => "pizzas";

	public Task<Created<Pizza>> HandleAsync(CreatePizzaRequest request, CancellationToken cancellationToken)
	{
		var pizza = store.Add(request.Name, request.Price);
		return Task.FromResult(TypedResults.Created($"/pizzas/{pizza.Id}", pizza));
	}
}
