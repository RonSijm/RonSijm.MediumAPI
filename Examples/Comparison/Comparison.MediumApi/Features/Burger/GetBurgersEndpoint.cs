using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Features.Burger;

internal sealed class GetBurgersEndpoint(BurgerStore store) : IGetEndpoint<Ok<IEnumerable<Burger>>>
{
	[StringSyntax("Route")]
	public string Pattern => "burgers";

	public Task<Ok<IEnumerable<Burger>>> HandleAsync(CancellationToken cancellationToken)
		=> Task.FromResult(TypedResults.Ok(store.GetAll()));
}
