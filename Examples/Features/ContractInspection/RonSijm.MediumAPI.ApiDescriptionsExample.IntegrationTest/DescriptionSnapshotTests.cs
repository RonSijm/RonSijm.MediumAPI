using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace RonSijm.MediumAPI.ApiDescriptionsExample.IntegrationTest;

public sealed class DescriptionSnapshotTests
{
    [Fact]
    public void Capture_reads_the_provider_once_and_preserves_described_values()
    {
        var description = Description();
        description.GroupName = "described-group";
        description.HttpMethod = "pOsT";
        description.RelativePath = "/Case/{key:long}/";
        description.ParameterDescriptions[0].Source = new BindingSource("CuStOm", "display only", isGreedy: false, isFromRequest: true);
        description.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "Application/Example+Json" });
        var provider = Provider(description);

        var snapshot = provider.GetMediumApiDescriptionSnapshot();
        var operation = Assert.Single(snapshot.Operations);
        var parameter = Assert.Single(operation.Parameters);

        Assert.Equal(1, provider.Reads);
        Assert.Equal("described-group", operation.GroupName);
        Assert.Equal("pOsT", operation.HttpMethod);
        Assert.Equal("/Case/{key:long}/", operation.RelativePath);
        Assert.Equal("ReadItem", operation.Name);
        Assert.Equal("key", parameter.Name);
        Assert.Equal("CuStOm", parameter.Source);
        Assert.Equal(typeof(long).FullName, parameter.Type);
        Assert.True(parameter.IsRequired);
        Assert.Equal(new[] { "Application/Example+Json" }, operation.RequestContentTypes);
        _ = snapshot.ToJson();
        Assert.Equal(1, provider.Reads);
    }

    [Fact]
    public void Snapshot_copies_nested_values_instead_of_retaining_mutable_provider_objects()
    {
        var description = Description();
        var parameter = description.ParameterDescriptions[0];
        var requestFormat = new ApiRequestFormat { MediaType = "application/json" };
        description.SupportedRequestFormats.Add(requestFormat);
        var response = Response(200, typeof(string), "application/json");
        description.SupportedResponseTypes.Add(response);
        var snapshot = Provider(description).GetMediumApiDescriptionSnapshot();
        var before = snapshot.ToJson();

        description.GroupName = "changed";
        description.HttpMethod = "DELETE";
        description.RelativePath = "changed";
        description.ActionDescriptor.EndpointMetadata.Add(new EndpointNameMetadata("ChangedName"));
        parameter.Name = "changed";
        parameter.Source = BindingSource.Header;
        parameter.Type = typeof(Guid);
        parameter.IsRequired = false;
        description.ParameterDescriptions.Clear();
        requestFormat.MediaType = "text/plain";
        description.SupportedRequestFormats.Clear();
        response.StatusCode = 500;
        response.IsDefaultResponse = true;
        response.Type = typeof(object);
        response.ApiResponseFormats[0].MediaType = "text/plain";
        response.ApiResponseFormats.Clear();
        description.SupportedResponseTypes.Clear();

        Assert.Equal(before, snapshot.ToJson());
        var captured = Assert.Single(snapshot.Operations);
        Assert.Single(captured.Parameters);
        Assert.Single(captured.RequestContentTypes);
        Assert.Single(captured.Responses);
        Assert.Equal("application/json", Assert.Single(captured.Responses[0].ContentTypes));
    }

    [Fact]
    public void Exposed_collections_cannot_mutate_the_captured_contract()
    {
        var description = Description();
        description.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "application/json" });
        description.SupportedResponseTypes.Add(Response(200, typeof(string), "application/json"));
        var snapshot = Provider(description).GetMediumApiDescriptionSnapshot();
        var operation = Assert.Single(snapshot.Operations);

        AssertReadOnly(snapshot.Operations);
        AssertReadOnly(operation.Parameters);
        AssertReadOnly(operation.RequestContentTypes);
        AssertReadOnly(operation.Responses);
        AssertReadOnly(operation.Responses[0].ContentTypes);
    }

    [Fact]
    public void Operation_order_and_provider_version_do_not_create_contract_differences()
    {
        var first = Description("first", "GET", "ReadFirst", "a");
        var second = Description("second", "POST", "CreateSecond", "b");
        var expected = Provider(1, first, second).GetMediumApiDescriptionSnapshot();
        var actual = Provider(99, second, first).GetMediumApiDescriptionSnapshot();

        Assert.True(MediumApiContractComparison.CompareApiDescriptions(expected, actual).IsEquivalent);
        Assert.Equal(expected.ToJson(), actual.ToJson());
    }

    [Fact]
    public void Equal_operation_keys_use_their_full_descriptions_for_deterministic_ordering()
    {
        var required = Description();
        var optional = Description();
        optional.ParameterDescriptions[0].IsRequired = false;
        var expected = Provider(required, optional).GetMediumApiDescriptionSnapshot();
        var actual = Provider(optional, required).GetMediumApiDescriptionSnapshot();

        Assert.Equal(2, expected.Operations.Count);
        Assert.True(MediumApiContractComparison.CompareApiDescriptions(expected, actual).IsEquivalent);
    }

    [Fact]
    public void Duplicate_descriptions_are_retained_rather_than_collapsed()
    {
        var description = Description();
        var once = Provider(description).GetMediumApiDescriptionSnapshot();
        var twice = Provider(description, description).GetMediumApiDescriptionSnapshot();

        Assert.Equal(2, twice.Operations.Count);
        var comparison = MediumApiContractComparison.CompareApiDescriptions(once, twice);
        Assert.False(comparison.IsEquivalent);
        Assert.Contains(comparison.Differences, difference => difference.Path == "/operations/1" && difference.Kind == MediumApiContractDifferenceKind.Added);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("source")]
    [InlineData("type")]
    [InlineData("isRequired")]
    public void Binding_contract_changes_are_visible_to_comparison(string field)
    {
        var expectedDescription = Description();
        var actualDescription = Description();
        var actualParameter = actualDescription.ParameterDescriptions[0];
        switch (field)
        {
            case "name": actualParameter.Name = "renamed"; break;
            case "source": actualParameter.Source = BindingSource.Header; break;
            case "type": actualParameter.Type = typeof(Guid); break;
            case "isRequired": actualParameter.IsRequired = false; break;
            default: throw new ArgumentOutOfRangeException(nameof(field));
        }

        var comparison = MediumApiContractComparison.CompareApiDescriptions(Provider(expectedDescription).GetMediumApiDescriptionSnapshot(), Provider(actualDescription).GetMediumApiDescriptionSnapshot());

        var difference = Assert.Single(comparison.Differences);
        Assert.Equal("/operations/0/parameters/0/" + field, difference.Path);
        Assert.Equal(MediumApiContractDifferenceKind.Changed, difference.Kind);
    }

    [Theory]
    [InlineData("parameters")]
    [InlineData("requestContentTypes")]
    [InlineData("responses")]
    [InlineData("responseContentTypes")]
    public void Nested_description_order_is_meaningful_and_is_not_normalized(string field)
    {
        var description = Description();
        description.ParameterDescriptions.Add(new ApiParameterDescription { Name = "limit", Source = BindingSource.Query, Type = typeof(int), IsRequired = false });
        description.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "application/json" });
        description.SupportedRequestFormats.Add(new ApiRequestFormat { MediaType = "application/example+json" });
        description.SupportedResponseTypes.Add(Response(200, typeof(string), "application/json", "text/plain"));
        description.SupportedResponseTypes.Add(Response(404, typeof(void)));
        var before = Provider(description).GetMediumApiDescriptionSnapshot();
        switch (field)
        {
            case "parameters": SwapFirstTwo(description.ParameterDescriptions); break;
            case "requestContentTypes": SwapFirstTwo(description.SupportedRequestFormats); break;
            case "responses": SwapFirstTwo(description.SupportedResponseTypes); break;
            case "responseContentTypes": SwapFirstTwo(description.SupportedResponseTypes[0].ApiResponseFormats); break;
            default: throw new ArgumentOutOfRangeException(nameof(field));
        }

        var comparison = MediumApiContractComparison.CompareApiDescriptions(before, Provider(description).GetMediumApiDescriptionSnapshot());

        Assert.False(comparison.IsEquivalent);
        Assert.Contains(comparison.Differences, difference => difference.Path.StartsWith("/operations/0/", StringComparison.Ordinal));
    }

    [Fact]
    public void Responses_retain_default_flags_types_and_ordered_nullable_media_values()
    {
        var description = Description();
        var response = Response(0, typeof(Dictionary<string, List<Guid?[]>>), "application/problem+json");
        response.IsDefaultResponse = true;
        response.ApiResponseFormats.Add(new ApiResponseFormat { MediaType = null! });
        description.SupportedResponseTypes.Add(response);
        var snapshot = Provider(description).GetMediumApiDescriptionSnapshot();
        var captured = Assert.Single(Assert.Single(snapshot.Operations).Responses);

        Assert.Equal(0, captured.StatusCode);
        Assert.True(captured.IsDefaultResponse);
        Assert.Equal("System.Collections.Generic.Dictionary`2[System.String,System.Collections.Generic.List`1[System.Nullable`1[System.Guid][]]]", captured.Type);
        Assert.Equal(new string?[] { "application/problem+json", null }, captured.ContentTypes);
        Assert.DoesNotContain("Version=", snapshot.ToJson());
        response.IsDefaultResponse = false;
        var comparison = MediumApiContractComparison.CompareApiDescriptions(snapshot, Provider(description).GetMediumApiDescriptionSnapshot());
        Assert.Contains(comparison.Differences, difference => difference.Path == "/operations/0/responses/0/isDefaultResponse");
    }

    [Fact]
    public void The_last_endpoint_name_marker_wins_without_using_display_or_route_names()
    {
        var description = Description();
        description.ActionDescriptor.DisplayName = "display text";
        description.ActionDescriptor.EndpointMetadata.Add(new UnexportedValue());
        description.ActionDescriptor.EndpointMetadata.Add(new EndpointNameMetadata("FinalName"));

        Assert.Equal("FinalName", Assert.Single(Provider(description).GetMediumApiDescriptionSnapshot().Operations).Name);
        description.ActionDescriptor.EndpointMetadata.Add(new NullableNameMetadata(null));
        Assert.Null(Assert.Single(Provider(description).GetMediumApiDescriptionSnapshot().Operations).Name);
    }

    [Fact]
    public void Null_description_values_remain_distinct_from_empty_or_case_changed_values()
    {
        var description = Description();
        description.GroupName = null;
        description.HttpMethod = null;
        description.RelativePath = null;
        description.ActionDescriptor.EndpointMetadata.Clear();
        description.ParameterDescriptions[0].Name = null!;
        description.ParameterDescriptions[0].Source = null!;
        description.ParameterDescriptions[0].Type = null!;
        var snapshot = Provider(description).GetMediumApiDescriptionSnapshot();
        var captured = Assert.Single(snapshot.Operations);

        Assert.Null(captured.GroupName);
        Assert.Null(captured.HttpMethod);
        Assert.Null(captured.RelativePath);
        Assert.Null(captured.Name);
        Assert.Null(captured.Parameters[0].Name);
        Assert.Null(captured.Parameters[0].Source);
        Assert.Null(captured.Parameters[0].Type);
        description.RelativePath = string.Empty;
        var comparison = MediumApiContractComparison.CompareApiDescriptions(snapshot, Provider(description).GetMediumApiDescriptionSnapshot());
        Assert.Equal("/operations/0/relativePath", Assert.Single(comparison.Differences).Path);
    }

    [Fact]
    public void Default_values_and_unselected_objects_are_not_inspected_or_serialized()
    {
        var description = Description();
        description.ParameterDescriptions[0].DefaultValue = new UnexportedValue();
        description.ActionDescriptor.EndpointMetadata.Add(new UnexportedValue());
        var snapshot = Provider(description).GetMediumApiDescriptionSnapshot();
        var json = snapshot.ToJson();
        using var document = JsonDocument.Parse(json);
        var parameter = document.RootElement.GetProperty("operations")[0].GetProperty("parameters")[0];

        Assert.False(parameter.TryGetProperty("defaultValue", out _));
        Assert.False(parameter.TryGetProperty("modelMetadata", out _));
        Assert.False(parameter.TryGetProperty("parameterDescriptor", out _));
        description.ParameterDescriptions[0].DefaultValue = 17;
        Assert.True(MediumApiContractComparison.CompareApiDescriptions(snapshot, Provider(description).GetMediumApiDescriptionSnapshot()).IsEquivalent);
    }

    [Fact]
    public void Empty_groups_return_an_empty_snapshot_without_inventing_descriptions()
    {
        var provider = new SnapshotProvider(new ApiDescriptionGroupCollection(Array.Empty<ApiDescriptionGroup>(), version: 1));

        var snapshot = provider.GetMediumApiDescriptionSnapshot();

        Assert.Empty(snapshot.Operations);
        Assert.Equal(1, provider.Reads);
        using var document = JsonDocument.Parse(snapshot.ToJson());
        Assert.Equal(0, document.RootElement.GetProperty("operations").GetArrayLength());
    }

    [Fact]
    public void Provider_failures_propagate_as_the_same_exception_without_a_snapshot()
    {
        var failure = new InvalidOperationException("Synthetic API Explorer failure.");
        var provider = new ThrowingProvider(failure);

        var observed = Assert.Throws<InvalidOperationException>(() => provider.GetMediumApiDescriptionSnapshot());

        Assert.Same(failure, observed);
    }

    [Fact]
    public void Explicit_capture_and_comparison_require_non_null_arguments()
    {
        IApiDescriptionGroupCollectionProvider? missing = null;
        var snapshot = Provider().GetMediumApiDescriptionSnapshot();

        Assert.Throws<ArgumentNullException>(() => missing!.GetMediumApiDescriptionSnapshot());
        Assert.Throws<ArgumentNullException>(() => MediumApiContractComparison.CompareApiDescriptions(null!, snapshot));
        Assert.Throws<ArgumentNullException>(() => MediumApiContractComparison.CompareApiDescriptions(snapshot, null!));
    }

    private static ApiDescription Description(string path = "items/{key:long}", string method = "GET", string name = "ReadItem", string? group = "items")
    {
        var description = new ApiDescription
        {
            GroupName = group,
            HttpMethod = method,
            RelativePath = path,
            ActionDescriptor = new ActionDescriptor { EndpointMetadata = new List<object> { new EndpointNameMetadata(name) } }
        };
        description.ParameterDescriptions.Add(new ApiParameterDescription { Name = "key", Source = BindingSource.Path, Type = typeof(long), IsRequired = true });
        return description;
    }

    private static ApiResponseType Response(int status, Type type, params string[] contentTypes)
    {
        var response = new ApiResponseType { StatusCode = status, Type = type };
        foreach (var mediaType in contentTypes)
        {
            response.ApiResponseFormats.Add(new ApiResponseFormat { MediaType = mediaType });
        }

        return response;
    }

    private static SnapshotProvider Provider(params ApiDescription[] descriptions) => Provider(1, descriptions);

    private static SnapshotProvider Provider(int version, params ApiDescription[] descriptions) => new(new ApiDescriptionGroupCollection(new[] { new ApiDescriptionGroup("collection group is not the operation group", descriptions) }, version));

    private static void SwapFirstTwo<T>(IList<T> values) => (values[0], values[1]) = (values[1], values[0]);

    private static void AssertReadOnly<T>(IReadOnlyList<T> values)
    {
        var collection = Assert.IsAssignableFrom<IList<T>>(values);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(collection.Clear);
    }

    private sealed class SnapshotProvider(ApiDescriptionGroupCollection collection) : IApiDescriptionGroupCollectionProvider
    {
        public int Reads { get; private set; }

        public ApiDescriptionGroupCollection ApiDescriptionGroups
        {
            get
            {
                Reads++;
                return collection;
            }
        }
    }

    private sealed class ThrowingProvider(Exception failure) : IApiDescriptionGroupCollectionProvider
    {
        public ApiDescriptionGroupCollection ApiDescriptionGroups => throw failure;
    }

    private sealed class NullableNameMetadata(string? name) : IEndpointNameMetadata
    {
        public string EndpointName { get; } = name!;
    }

    private sealed class UnexportedValue
    {
        public string Value => throw new InvalidOperationException("Unselected values must not be inspected.");
        public override string ToString() => throw new InvalidOperationException("Unselected values must not be formatted.");
    }
}