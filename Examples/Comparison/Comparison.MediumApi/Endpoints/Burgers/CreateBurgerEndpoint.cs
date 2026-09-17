using System.Diagnostics.CodeAnalysis;
using Comparison.MediumApi.Data;
using Comparison.MediumApi.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using RonSijm.MediumAPI;

namespace Comparison.MediumApi.Endpoints.Burgers;

internal sealed class CreateBurgerEndpoint : IEndpointAdapter
{
	private readonly BurgerStore store;

	public CreateBurgerEndpoint(BurgerStore store) => this.store = store;

	public HttpVerb Verb => HttpVerb.Post;

	[StringSyntax("Route")]
	public string Pattern => "burgers";

	public Task<Created<Burger>> HandleAsync(CreateBurgerRequest request, CancellationToken cancellationToken)
	{
		var burger = store.Add(request.Name, request.Price);
		return Task.FromResult(TypedResults.Created($"/burgers/{burger.Id}", burger));
	}
}
