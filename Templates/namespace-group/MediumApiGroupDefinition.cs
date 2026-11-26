using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using RonSijm.MediumAPI;

namespace MediumApi.TemplateNamespace;

internal sealed class MediumApiGroupDefinition : INamespaceEndpointGroup
{
	public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
	{
		return endpoints.MapGroup("__MediumApiPrefix__");
	}
}
