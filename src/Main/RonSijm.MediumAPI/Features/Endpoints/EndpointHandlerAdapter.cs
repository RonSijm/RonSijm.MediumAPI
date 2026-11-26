using System.ComponentModel;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RonSijm.MediumAPI;

/// <summary>Infrastructure used by generated registrations to retain the original handler pipeline and manage endpoint activation and default-instance ownership.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class EndpointHandlerAdapter
{
    /// <summary>Maps the original method through ASP.NET Core's route-handler infrastructure without creating an endpoint at startup.</summary>
    public static RouteHandlerBuilder Map<TEndpoint>(IEndpointRouteBuilder builder, string pattern, string httpMethod) where TEndpoint : class, IEndpoint
    {
        var method = typeof(TEndpoint).GetMethod("HandleAsync")
                     ?? throw new InvalidOperationException($"Endpoint '{typeof(TEndpoint).FullName}' has no public HandleAsync method.");
        // An open instance delegate retains the original MethodInfo without constructing a target.
        // The framework uses it for metadata; our factory supplies the per-request target for invocation.
        var signature = new[] { method.DeclaringType! }.Concat(method.GetParameters().Select(parameter => parameter.ParameterType)).Append(method.ReturnType).ToArray();
        var handler = method.CreateDelegate(Expression.GetDelegateType(signature));
        var fromServices = typeof(TEndpoint).IsDefined(typeof(EndpointFromServicesAttribute), inherit: false);
        var headers = ResponseHeaderDeclarations.Read(typeof(TEndpoint), inherit: false)
            .Concat(ResponseHeaderDeclarations.Read(method, inherit: true)).Select(header => header.Snapshot()).ToArray();
        var mapping = RouteHandlerServices.Map(builder, pattern, handler, [httpMethod], (handlerMethod, options) =>
            {
                options!.EndpointBuilder!.Metadata.Add(new MediumApiEndpointMetadata(typeof(TEndpoint)));
                EndpointClassMetadata.Apply(typeof(TEndpoint), options!.EndpointBuilder!);
                return RequestDelegateFactory.InferMetadata(handlerMethod, options);
            },
            (_, options, metadata) => CreateRequestDelegate<TEndpoint>(method, options, metadata, headers, fromServices));
        EndpointResponseDeclarations.ApplyMethodResponseSets(method, mapping);
        MediumApiEndpointNameExtensions.ApplyDefaultEndpointName<TEndpoint>(mapping);
        EndpointAntiforgeryConventions.ApplyDeclaredDisable(typeof(TEndpoint), mapping);
        EndpointResponseFallbackDeclarations.ApplyDeclaredFallbacks(typeof(TEndpoint), mapping);
        foreach (var declaration in typeof(TEndpoint).GetCustomAttributesData())
        {
            if (!typeof(EndpointFilterAttribute).IsAssignableFrom(declaration.AttributeType) && !typeof(EndpointConventionAttribute).IsAssignableFrom(declaration.AttributeType))
            {
                continue;
            }

            var attribute = EndpointAttributeMetadata.Create(declaration);
            if (attribute is EndpointFilterAttribute filter)
            {
                filter.Apply(mapping);
            }
            else
            {
                ((EndpointConventionAttribute)attribute).Apply(mapping);
            }
        }

        foreach (var attribute in method.GetCustomAttributes(inherit: true))
        {
            if (attribute is EndpointResponsesAttribute)
            {
                continue;
            }

            if (attribute is EndpointFilterAttribute filter)
            {
                filter.Apply(mapping);
            }

            if (attribute is EndpointConventionAttribute convention)
            {
                convention.Apply(mapping);
            }

            if (attribute is EndpointResponseFallbackAttribute fallback)
            {
                fallback.Apply(mapping);
            }

            if (attribute is DisableEndpointAntiforgeryAttribute disable)
            {
                disable.Apply(mapping);
            }

            if (attribute is IEndpointNameMetadata { EndpointName: not null } name)
            {
                mapping.WithName(name.EndpointName);
            }
        }

        return mapping;
    }

    private static RequestDelegateResult CreateRequestDelegate<TEndpoint>(System.Reflection.MethodInfo method, RequestDelegateFactoryOptions options, RequestDelegateMetadataResult? metadata, ResponseHeaderPolicy[] headers, bool fromServices) where TEndpoint : class, IEndpoint
    {
        var activationKey = new object();
        // Install handler declarations after ordinary filters have been composed, so a
        // short circuit or binding failure does not receive a header from an uncalled handler.
        if (headers.Length > 0)
        {
            options.EndpointBuilder!.FilterFactories.Add((_, next) => context => context.HttpContext.Response.StatusCode >= StatusCodes.Status400BadRequest
                ? next(context)
                : ResponseHeaderPolicy.InvokeAsync(headers, context, next));
        }

        var result = RequestDelegateFactory.Create(method, context => ((Activation<TEndpoint>)context.Items[activationKey]!).GetOrCreate(context), options, metadata);
        return new RequestDelegateResult(InvokeAsync, result.EndpointMetadata);

        async Task InvokeAsync(HttpContext context)
        {
            var activation = new Activation<TEndpoint>(fromServices);
            var hadPrevious = context.Items.TryGetValue(activationKey, out var previous);
            context.Items[activationKey] = activation;
            try { await result.RequestDelegate(context); }
            finally
            {
                try { await activation.DisposeAsync(); }
                finally
                {
                    if (hadPrevious)
                    {
                        context.Items[activationKey] = previous;
                    }
                    else
                    {
                        context.Items.Remove(activationKey);
                    }
                }
            }
        }
    }

    private sealed class Activation<TEndpoint>(bool fromServices) where TEndpoint : class, IEndpoint
    {
        private TEndpoint? instance;
        public TEndpoint GetOrCreate(HttpContext context) => instance ??= fromServices ? context.RequestServices.GetRequiredService<TEndpoint>() : ActivatorUtilities.CreateInstance<TEndpoint>(context.RequestServices);

        public async ValueTask DisposeAsync()
        {
            if (fromServices)
            {
                return;
            }

            if (instance is IAsyncDisposable asynchronous)
            {
                await asynchronous.DisposeAsync();
            }
            else if (instance is IDisposable synchronous)
            {
                synchronous.Dispose();
            }
        }
    }
}