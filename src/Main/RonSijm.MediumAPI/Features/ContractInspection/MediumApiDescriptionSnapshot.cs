using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>
/// A read-only snapshot of descriptions supplied by an explicitly selected API Explorer provider. It describes
/// documentation metadata, not actual route exposure, schemas, policy execution or complete HTTP compatibility.
/// Reading a provider can materialize its normal providers/conventions and can fail; capture does not map routes,
/// resolve application services, invoke request handlers, publish diagnostics or retain the provider.
/// </summary>
public sealed class MediumApiDescriptionSnapshot
{
    private MediumApiDescriptionSnapshot(MediumApiOperationDescription[] operations) => Operations = Array.AsReadOnly(operations);

    /// <summary>Described operations sorted deterministically, with duplicate descriptions retained.</summary>
    public IReadOnlyList<MediumApiOperationDescription> Operations { get; }

    /// <summary>Serializes this snapshot without writing files or logging description values.</summary>
    public string ToJson(bool writeIndented = true) => JsonSerializer.Serialize(this, MediumApiContractSnapshot.CreateJsonOptions(writeIndented));

    internal static MediumApiDescriptionSnapshot Capture(IApiDescriptionGroupCollectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        var groups = provider.ApiDescriptionGroups;
        var operations = groups.Items.SelectMany(group => group.Items)
            .Select(description => new MediumApiOperationDescription(description))
            .OrderBy(operation => operation.GroupName, StringComparer.Ordinal)
            .ThenBy(operation => operation.RelativePath, StringComparer.Ordinal)
            .ThenBy(operation => operation.HttpMethod, StringComparer.Ordinal)
            .ThenBy(operation => operation.Name, StringComparer.Ordinal)
            .ThenBy(operation => JsonSerializer.Serialize(operation, MediumApiContractSnapshot.CreateJsonOptions(writeIndented: false)), StringComparer.Ordinal)
            .ToArray();
        return new(operations);
    }
}

/// <summary>
/// One provider-described operation. Paths, method casing and nested collection order are retained as supplied.
/// The name is the last endpoint-name metadata value, not an inferred OpenAPI operation identifier.
/// </summary>
public sealed class MediumApiOperationDescription
{
    internal MediumApiOperationDescription(ApiDescription description)
    {
        GroupName = description.GroupName;
        HttpMethod = description.HttpMethod;
        RelativePath = description.RelativePath;
        Name = description.ActionDescriptor.EndpointMetadata.OfType<IEndpointNameMetadata>().LastOrDefault()?.EndpointName;
        Parameters = Array.AsReadOnly(description.ParameterDescriptions.Select(parameter => new MediumApiParameterDescription(parameter)).ToArray());
        RequestContentTypes = Array.AsReadOnly(description.SupportedRequestFormats.Select(format => (string?)format.MediaType).ToArray());
        Responses = Array.AsReadOnly(description.SupportedResponseTypes.Select(response => new MediumApiResponseDescription(response)).ToArray());
    }

    public string? GroupName { get; }
    public string? HttpMethod { get; }
    public string? RelativePath { get; }
    public string? Name { get; }
    public IReadOnlyList<MediumApiParameterDescription> Parameters { get; }
    public IReadOnlyList<string?> RequestContentTypes { get; }
    public IReadOnlyList<MediumApiResponseDescription> Responses { get; }
}

/// <summary>
/// A parameter as described by API Explorer. Source is the supplied binding-source identifier and Type is the
/// provider's CLR type description, not a reconstructed handler signature or JSON schema. Default values,
/// model metadata, parameter descriptors and application objects are not retained.
/// </summary>
public sealed class MediumApiParameterDescription
{
    internal MediumApiParameterDescription(ApiParameterDescription parameter)
    {
        Name = parameter.Name;
        Source = parameter.Source?.Id;
        Type = MediumApiContractSnapshot.DescribeType(parameter.Type);
        IsRequired = parameter.IsRequired;
    }

    public string? Name { get; }
    public string? Source { get; }
    public string? Type { get; }
    public bool IsRequired { get; }
}

/// <summary>One provider-described response, retaining response and media-type order without formatter or model-metadata references.</summary>
public sealed class MediumApiResponseDescription
{
    internal MediumApiResponseDescription(ApiResponseType response)
    {
        StatusCode = response.StatusCode;
        IsDefaultResponse = response.IsDefaultResponse;
        Type = MediumApiContractSnapshot.DescribeType(response.Type);
        ContentTypes = Array.AsReadOnly(response.ApiResponseFormats.Select(format => (string?)format.MediaType).ToArray());
    }

    public int StatusCode { get; }
    public bool IsDefaultResponse { get; }
    public string? Type { get; }
    public IReadOnlyList<string?> ContentTypes { get; }
}