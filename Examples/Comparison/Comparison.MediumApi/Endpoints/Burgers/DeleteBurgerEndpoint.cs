using System.Diagnostics.CodeAnalysis;
using Comparison.MediumApi.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Endpoints.Burgers;

internal sealed class DeleteBurgerEndpoint : IEndpointAdapter
{
	private readonly BurgerStore store;

	public DeleteBurgerEndpoint(BurgerStore store) => this.store = store;

	public HttpVerb Verb => HttpVerb.Delete;

	[StringSyntax("Route")]
	public string Pattern => "burgers/{id:int}";

	public Task<Results<NoContent, NotFound>> HandleAsync(int id, CancellationToken cancellationToken)
		=> Task.FromResult<Results<NoContent, NotFound>>(store.Delete(id) ? TypedResults.NoContent() : TypedResults.NotFound());
}
