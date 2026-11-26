using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace RonSijm.MediumAPI;

/// <summary>Response-header conventions usable by native endpoints, groups and shared filter configuration.</summary>
public static class MediumApiResponseHeaderExtensions
{
    /// <summary>Registers one replacing header at this point in the native filter pipeline. An optional result type delays it until a matching result is returned.</summary>
    public static TBuilder WithResponseHeader<TBuilder>(this TBuilder builder, string name, string value, Type? resultType = null) where TBuilder : IEndpointConventionBuilder
        => builder.WithResponseHeader(new ResponseHeaderAttribute(name, value) { ResultType = resultType });

    /// <summary>Snapshots a constant header declaration, including multiple values, and registers it using the native filter pipeline.</summary>
    public static TBuilder WithResponseHeader<TBuilder>(this TBuilder builder, ResponseHeaderAttribute header) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(header);
        var policies = new[] { header.Snapshot() };
        return builder.AddEndpointFilterFactory((_, next) => context => ResponseHeaderPolicy.InvokeAsync(policies, context, next));
    }

    /// <summary>Registers each child as a separate ordinary header convention in order. This preserves the existing native filter unwind order for selected-result headers; a later failure does not undo earlier registrations.</summary>
    public static TBuilder WithResponseHeaders<TBuilder>(this TBuilder builder, ResponseHeadersAttribute headers) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(headers);
        foreach (var header in headers.Headers) { builder.WithResponseHeader(header); }

        return builder;
    }

    internal static void ApplyDeclaredHeaders(Type type, IEndpointConventionBuilder builder)
    {
        foreach (var header in ResponseHeaderDeclarations.Read(type, inherit: false))
        {
            builder.WithResponseHeader(header);
        }
    }
}