using System.Diagnostics.CodeAnalysis;
using Comparison.MediumApi.Data;
using Comparison.MediumApi.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Endpoints.Burgers;

internal sealed class GetBurgersEndpoint : IEndpointAdapter
{
	private readonly BurgerStore store;

	public GetBurgersEndpoint(BurgerStore store) => this.store = store;

	public HttpVerb Verb => HttpVerb.Get;

	[StringSyntax("Route")]
	public string Pattern => "burgers";

	public Task<Ok<IEnumerable<Burger>>> HandleAsync(CancellationToken cancellationToken)
		=> Task.FromResult(TypedResults.Ok(store.GetAll()));
}
