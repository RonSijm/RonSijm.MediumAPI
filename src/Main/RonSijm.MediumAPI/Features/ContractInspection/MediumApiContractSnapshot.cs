using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>
/// A read-only snapshot of selected standard metadata on actual composed routes. This is not a schema,
/// binding, policy-execution or complete HTTP compatibility proof. Reading data sources may execute their
/// normal endpoint conventions; capture does not add routes or invoke request handlers.
/// </summary>
public sealed class MediumApiContractSnapshot
{
    private MediumApiContractSnapshot(MediumApiRouteContract[] routes) => Routes = Array.AsReadOnly(routes);

    public IReadOnlyList<MediumApiRouteContract> Routes { get; }

    /// <summary>Serializes the snapshot without writing a file or logging any contract values.</summary>
    public string ToJson(bool writeIndented = true) => JsonSerializer.Serialize(this, CreateJsonOptions(writeIndented));

    internal static JsonSerializerOptions CreateJsonOptions(bool writeIndented) => new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = writeIndented };

    internal static MediumApiContractSnapshot Capture(IEndpointRouteBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return CaptureRoutes(builder.DataSources.SelectMany(source => source.Endpoints));
    }

    internal static MediumApiContractSnapshot Capture(EndpointDataSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return CaptureRoutes(source.Endpoints);
    }

    private static MediumApiContractSnapshot CaptureRoutes(IEnumerable<Microsoft.AspNetCore.Http.Endpoint> endpoints)
    {
        var routes = endpoints.OfType<RouteEndpoint>().Distinct()
            .Select(endpoint => new MediumApiRouteContract(endpoint))
            .OrderBy(route => route.Pattern, StringComparer.Ordinal)
            .ThenBy(route => string.Join("\u001f", route.HttpMethods), StringComparer.Ordinal)
            .ThenBy(route => route.Name, StringComparer.Ordinal)
            .ThenBy(route => route.Order)
            .ThenBy(route => JsonSerializer.Serialize(route, CreateJsonOptions(writeIndented: false)), StringComparer.Ordinal)
            .ToArray();
        return new(routes);
    }

    internal static string? DescribeType(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        if (type.IsArray)
        {
            return DescribeType(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        }

        if (type.IsByRef)
        {
            return DescribeType(type.GetElementType()) + "&";
        }

        if (type.IsPointer)
        {
            return DescribeType(type.GetElementType()) + "*";
        }

        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var name = type.GetGenericTypeDefinition().FullName ?? type.Name;
        return name + "[" + string.Join(",", type.GetGenericArguments().Select(DescribeType)) + "]";
    }
}

/// <summary>Standard declared metadata for one route, with metadata-array ordering retained.</summary>
public sealed class MediumApiRouteContract
{
    internal MediumApiRouteContract(RouteEndpoint endpoint)
    {
        Pattern = endpoint.RoutePattern.RawText;
        HttpMethods = Array.AsReadOnly((endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? []).ToArray());
        Name = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
        Order = endpoint.Order;
        Authorization = Array.AsReadOnly(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Select(data => new MediumApiAuthorizationContract(data.Policy, data.Roles, data.AuthenticationSchemes)).ToArray());
        AllowsAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        RequiresAntiforgery = endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation;
        var accepts = endpoint.Metadata.GetMetadata<IAcceptsMetadata>();
        Accepts = accepts is null ? null : new MediumApiAcceptsContract(MediumApiContractSnapshot.DescribeType(accepts.RequestType), accepts.IsOptional, accepts.ContentTypes.ToArray());
        Responses = Array.AsReadOnly(endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()
            .Select(metadata => new MediumApiResponseContract(metadata.StatusCode, MediumApiContractSnapshot.DescribeType(metadata.Type), metadata.ContentTypes.ToArray())).ToArray());
        Tags = Array.AsReadOnly((endpoint.Metadata.GetMetadata<ITagsMetadata>()?.Tags ?? []).ToArray());
        Summary = endpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>()?.Summary;
        Description = endpoint.Metadata.GetMetadata<IEndpointDescriptionMetadata>()?.Description;
    }

    public string? Pattern { get; }

    /// <summary>An empty collection represents unrestricted HTTP-method matching.</summary>
    public IReadOnlyList<string> HttpMethods { get; }

    public string? Name { get; }
    public int Order { get; }
    public IReadOnlyList<MediumApiAuthorizationContract> Authorization { get; }
    public bool AllowsAnonymous { get; }
    public bool? RequiresAntiforgery { get; }
    public MediumApiAcceptsContract? Accepts { get; }

    /// <summary>Declared IProducesResponseTypeMetadata only; MVC/custom provider and JSON-schema inference are not performed.</summary>
    public IReadOnlyList<MediumApiResponseContract> Responses { get; }

    public IReadOnlyList<string> Tags { get; }
    public string? Summary { get; }
    public string? Description { get; }
}

public sealed class MediumApiAuthorizationContract
{
    internal MediumApiAuthorizationContract(string? policy, string? roles, string? authenticationSchemes)
    {
        Policy = policy;
        Roles = roles;
        AuthenticationSchemes = authenticationSchemes;
    }

    public string? Policy { get; }
    public string? Roles { get; }
    public string? AuthenticationSchemes { get; }
}

public sealed class MediumApiAcceptsContract
{
    internal MediumApiAcceptsContract(string? type, bool isOptional, string[] contentTypes)
    {
        Type = type;
        IsOptional = isOptional;
        ContentTypes = Array.AsReadOnly(contentTypes);
    }

    public string? Type { get; }
    public bool IsOptional { get; }
    public IReadOnlyList<string> ContentTypes { get; }
}

public sealed class MediumApiResponseContract
{
    internal MediumApiResponseContract(int statusCode, string? type, string[] contentTypes)
    {
        StatusCode = statusCode;
        Type = type;
        ContentTypes = Array.AsReadOnly(contentTypes);
    }

    public int StatusCode { get; }
    public string? Type { get; }
    public IReadOnlyList<string> ContentTypes { get; }
}