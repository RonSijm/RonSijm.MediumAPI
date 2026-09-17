using System.Diagnostics.CodeAnalysis;
using Comparison.MediumApi.Data;
using Comparison.MediumApi.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Endpoints.Burgers;

internal sealed class GetBurgerByIdEndpoint : IEndpointAdapter
{
	private readonly BurgerStore store;

	public GetBurgerByIdEndpoint(BurgerStore store) => this.store = store;

	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "burgers/{id:int}";

	public Task<Results<Ok<Burger>, NotFound>> HandleAsync(int id, CancellationToken cancellationToken)
	{
		var burger = store.GetById(id);
		return Task.FromResult<Results<Ok<Burger>, NotFound>>(burger is null ? TypedResults.NotFound() : TypedResults.Ok(burger));
	}
}
