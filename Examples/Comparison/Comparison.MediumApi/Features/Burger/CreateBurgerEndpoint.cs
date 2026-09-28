using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Features.Burger;

internal sealed class CreateBurgerEndpoint(BurgerStore store) : IPostEndpoint<CreateBurgerRequest, Created<Burger>>
{
	[StringSyntax("Route")]
	public string Pattern => "burgers";

	public Task<Created<Burger>> HandleAsync(CreateBurgerRequest request, CancellationToken cancellationToken)
	{
		var burger = store.Add(request.Name, request.Price);
		return Task.FromResult(TypedResults.Created($"/burgers/{burger.Id}", burger));
	}
}
