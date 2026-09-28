using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Features.Burger;

internal sealed class DeleteBurgerEndpoint(BurgerStore store) : IDeleteEndpoint<int, Results<NoContent, NotFound>>
{
	[StringSyntax("Route")]
	public string Pattern => "burgers/{id:int}";

	public Task<Results<NoContent, NotFound>> HandleAsync(int id, CancellationToken cancellationToken)
		=> Task.FromResult<Results<NoContent, NotFound>>(store.Delete(id) ? TypedResults.NoContent() : TypedResults.NotFound());
}
