using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Features.Burger;

internal sealed class GetBurgerByIdEndpoint(BurgerStore store) : IGetEndpoint<int, Results<Ok<Burger>, NotFound>>
{
	[StringSyntax("Route")]
	public string Pattern => "burgers/{id:int}";

	public Task<Results<Ok<Burger>, NotFound>> HandleAsync(int id, CancellationToken cancellationToken)
	{
		var burger = store.GetById(id);
		return Task.FromResult<Results<Ok<Burger>, NotFound>>(burger is null ? TypedResults.NotFound() : TypedResults.Ok(burger));
	}
}
