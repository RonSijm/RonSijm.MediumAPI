using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.ServiceEndpointsExample.Endpoints.Api
{
    public sealed class ServiceGroupDefinition : INamespaceEndpointGroup
    {
        public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/services");
    }

    [EndpointFromServices]
    public sealed class GetScopedServiceEndpoint(ServiceProbe probe, RequestDependency dependency) : ServiceTarget(probe, "scoped", dependency.Id, "container"), IGetEndpoint, IAsyncDisposable
    {
        [StringSyntax("Route")]
        public string Pattern => "/scoped/{value:int}";

        public async Task<Ok<ServiceReceipt>> HandleAsync([FromRoute] int value, HttpContext context)
        {
            if (value == -1)
            {
                throw new ServiceFailureException();
            }

            if (value == -2)
            {
                Probe.HandlerEntered.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted);
            }

            return TypedResults.Ok(Receipt(value, ReferenceEquals(this, context.RequestServices.GetRequiredService<GetScopedServiceEndpoint>())));
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            DisposeTarget();
        }
    }

    [EndpointFromServices]
    public sealed class GetFactoryServiceEndpoint(ServiceProbe probe, RequestDependency dependency, string configuredName) : ServiceTarget(probe, "factory", dependency.Id, configuredName), IGetEndpoint, IAsyncDisposable
    {
        [StringSyntax("Route")]
        public string Pattern => "/factory/{value:int}";

        public Ok<ServiceReceipt> HandleAsync([FromRoute] int value, HttpContext context) => TypedResults.Ok(Receipt(value, ReferenceEquals(this, context.RequestServices.GetRequiredService<GetFactoryServiceEndpoint>())));

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            DisposeTarget();
        }
    }

    [EndpointFromServices]
    public sealed class GetTransientServiceEndpoint(ServiceProbe probe, RequestDependency dependency) : ServiceTarget(probe, "transient", dependency.Id, "container"), IGetEndpoint, IDisposable
    {
        [StringSyntax("Route")]
        public string Pattern => "/transient/{value:int}";

        public Ok<ServiceReceipt> HandleAsync([FromRoute] int value, HttpContext context) => TypedResults.Ok(Receipt(value, DependencyId == context.RequestServices.GetRequiredService<RequestDependency>().Id));
        public void Dispose() => DisposeTarget();
    }

    [EndpointFromServices]
    public sealed class GetSingletonServiceEndpoint(ServiceProbe probe) : ServiceTarget(probe, "singleton", 0, "container"), IGetEndpoint, IDisposable
    {
        [StringSyntax("Route")]
        public string Pattern => "/singleton/{value:int}";

        public Ok<ServiceReceipt> HandleAsync([FromRoute] int value, HttpContext context) => TypedResults.Ok(Receipt(value, ReferenceEquals(this, context.RequestServices.GetRequiredService<GetSingletonServiceEndpoint>())));
        public void Dispose() => DisposeTarget();
    }

    [EndpointFromServices]
    public sealed class GetExternalServiceEndpoint(ServiceProbe probe) : ServiceTarget(probe, "external", 0, "caller"), IGetEndpoint, IDisposable
    {
        [StringSyntax("Route")]
        public string Pattern => "/external/{value:int}";

        public Ok<ServiceReceipt> HandleAsync([FromRoute] int value, HttpContext context) => TypedResults.Ok(Receipt(value, ReferenceEquals(this, context.RequestServices.GetRequiredService<GetExternalServiceEndpoint>())));
        public void Dispose() => DisposeTarget();
    }

    public sealed class GetDefaultServiceEndpoint(ServiceProbe probe, string origin = "adapter") : ServiceTarget(probe, "default", 0, origin), IGetEndpoint, IDisposable
    {
        [StringSyntax("Route")]
        public string Pattern => "/default/{value:int}";

        public Ok<ServiceReceipt> HandleAsync([FromRoute] int value, HttpContext context) => TypedResults.Ok(Receipt(value, false));
        public void Dispose() => DisposeTarget();
    }

    [EndpointFromServices]
    public sealed class GetDeferredServiceEndpoint(ServiceProbe probe, RequestDependency dependency) : ServiceTarget(probe, "deferred", dependency.Id, "container"), IGetEndpoint, IAsyncDisposable
    {
        [StringSyntax("Route")]
        public string Pattern => "/deferred/{value:int}";

        public ServiceDeferredResult HandleAsync([FromRoute] int value, HttpContext context, [FromQuery] bool hold = false) => new(this, Probe, Receipt(value, ReferenceEquals(this, context.RequestServices.GetRequiredService<GetDeferredServiceEndpoint>())), hold);

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            DisposeTarget();
        }
    }
}

namespace RonSijm.MediumAPI.ServiceEndpointsExample.Endpoints.Manual
{
    [EndpointFromServices]
    public sealed class ManualServiceEndpoint(ServiceProbe probe, RequestDependency dependency) : ServiceTarget(probe, "manual", dependency.Id, "container"), IGetEndpoint, IManuallyMappedEndpoint, IAsyncDisposable
    {
        [StringSyntax("Route")]
        public string Pattern => "/manual/{value:int}";

        public Ok<ServiceReceipt> HandleAsync([FromRoute] int value, HttpContext context) => TypedResults.Ok(Receipt(value, ReferenceEquals(this, context.RequestServices.GetRequiredService<ManualServiceEndpoint>())));

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            DisposeTarget();
        }
    }
}