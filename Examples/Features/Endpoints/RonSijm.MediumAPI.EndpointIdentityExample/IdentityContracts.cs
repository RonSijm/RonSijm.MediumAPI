using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.EndpointIdentityExample;

public sealed record IdentityResponse(string Source, int Value);

public sealed record IdentityFilterObservation(Type? EndpointType, Type? HandlerDeclaringType, int BoundValue);

public sealed record IdentityProviderObservation(Type? EndpointType, Type? HandlerDeclaringType);

public sealed class IdentityProbe
{
    private readonly ConcurrentDictionary<Type, int> _activations = new();
    private readonly ConcurrentDictionary<Type, int> _disposals = new();
    private readonly ConcurrentDictionary<string, int> _calls = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<IdentityFilterObservation> _filters = new();

    public int Activations => _activations.Values.Sum();
    public int Disposals => _disposals.Values.Sum();
    public int Calls => _calls.Values.Sum();
    public IReadOnlyList<IdentityFilterObservation> FilterObservations => _filters.ToArray();
    public int ActivationsFor(Type type) => _activations.GetValueOrDefault(type);
    public int DisposalsFor(Type type) => _disposals.GetValueOrDefault(type);
    public int CallsFor(string source) => _calls.GetValueOrDefault(source);
    public void RecordActivation(Type type) => _activations.AddOrUpdate(type, 1, (_, count) => count + 1);
    public void RecordDisposal(Type type) => _disposals.AddOrUpdate(type, 1, (_, count) => count + 1);
    public void RecordFilter(IdentityFilterObservation observation) => _filters.Enqueue(observation);

    public IdentityResponse RecordCall(string source, int value)
    {
        _calls.AddOrUpdate(source, 1, (_, count) => count + 1);
        return new(source, value);
    }
}

public static class IdentityFilters
{
    public static EndpointFilterDelegate Observe(EndpointFilterFactoryContext factoryContext, EndpointFilterDelegate next)
    {
        var declaringType = factoryContext.MethodInfo.DeclaringType;
        return async invocation =>
        {
            var type = invocation.HttpContext.GetEndpoint()?.Metadata.GetMetadata<MediumApiEndpointMetadata>()?.EndpointType;
            invocation.HttpContext.RequestServices.GetRequiredService<IdentityProbe>().RecordFilter(new(type, declaringType, invocation.GetArgument<int>(0)));
            return await next(invocation);
        };
    }
}

// Native metadata providers can inspect identity during RequestDelegateFactory inference.
// This observation remains server-side metadata; the HTTP payload is the same on native routes.
public sealed class IdentityMetadataResult(IdentityResponse response) : IResult, IEndpointMetadataProvider
{
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new IdentityProviderObservation(builder.Metadata.OfType<MediumApiEndpointMetadata>().LastOrDefault()?.EndpointType, method.DeclaringType));
        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(IdentityResponse), ["application/json"]));
    }

    public Task ExecuteAsync(HttpContext context) => TypedResults.Ok(response).ExecuteAsync(context);
}

public sealed class NativeIdentityHandler
{
    public Task<Ok<IdentityResponse>> HandleAsync([FromRoute] int value, IdentityProbe probe) => Task.FromResult(TypedResults.Ok(probe.RecordCall("native", value)));
}