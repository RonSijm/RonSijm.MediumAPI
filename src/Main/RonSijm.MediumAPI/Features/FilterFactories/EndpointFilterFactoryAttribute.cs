using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>Registers a static native filter factory on an endpoint, handler or namespace definition. Repeated declarations register repeated factories.</summary>
public class EndpointFilterFactoryAttribute<TFactory> : EndpointFilterAttribute where TFactory : IMediumApiEndpointFilterFactory
{
    internal override void Apply(RouteHandlerBuilder endpoint)
    {
        endpoint.AddMediumApiFilterFactory<TFactory>();
    }

    internal override void Apply(RouteGroupBuilder group)
    {
        group.AddMediumApiFilterFactory<TFactory>();
    }
}