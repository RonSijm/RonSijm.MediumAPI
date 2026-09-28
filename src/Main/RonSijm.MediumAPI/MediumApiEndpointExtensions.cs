using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RonSijm.MediumAPI;

/// <summary>
/// The single entry point applications call to wire up MediumAPI endpoints, regardless of how many projects
/// those endpoints are spread across.
/// </summary>
public static class MediumApiEndpointExtensions
{
	/// <summary>
	/// Reserved for future DI wiring. Currently a no-op - endpoint adapters are activated per-request via
	/// <see cref="Microsoft.Extensions.DependencyInjection.ActivatorUtilities"/>, so nothing needs to be
	/// registered up front.
	/// </summary>
	public static IServiceCollection AddMediumApiEndpoints(this IServiceCollection services) => services;

	/// <summary>
	/// Maps every <see cref="IEndpoint"/> that a compile-time source generator has discovered anywhere
	/// in the application's assembly graph. Call this once, from the host project - endpoints declared in
	/// referenced projects (e.g. per-domain <c>*.ASP</c> projects) are included automatically.
	/// </summary>
	public static IEndpointRouteBuilder MapMediumApiEndpoints(this IEndpointRouteBuilder builder)
	{
		EndpointRegistry.EnsureReferencedAssembliesAreLoaded();

		foreach (var mapEndpoints in EndpointRegistry.Mappers)
		{
			mapEndpoints(builder);
		}

		return builder;
	}
}
