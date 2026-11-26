using System.Text.Json;

namespace RonSijm.MediumAPI;

/// <summary>The structural change at a JSON Pointer. Added/removed values are distinguished from explicit JSON null.</summary>
public enum MediumApiContractDifferenceKind
{
    Added,
    Removed,
    Changed
}

/// <summary>
/// One difference. Values are JSON fragments supplied by the caller and may contain documentation content;
/// they are never logged automatically. A null value means the side is absent, not an explicit JSON null.
/// </summary>
public sealed class MediumApiContractDifference
{
    internal MediumApiContractDifference(string path, MediumApiContractDifferenceKind kind, string? expectedJson, string? actualJson)
    {
        Path = path;
        Kind = kind;
        ExpectedJson = expectedJson;
        ActualJson = actualJson;
    }

    public string Path { get; }
    public MediumApiContractDifferenceKind Kind { get; }
    public string? ExpectedJson { get; }
    public string? ActualJson { get; }
}

/// <summary>
/// Explicit structural comparison of contract snapshots or JSON documents. Object property ordering is
/// ignored; array ordering, strings, booleans, nulls and number representations are preserved. No document
/// values, schemas, security requirements or tag arrays are silently normalized or discarded.
/// </summary>
public sealed class MediumApiContractComparison
{
    private MediumApiContractComparison(MediumApiContractDifference[] differences) => Differences = Array.AsReadOnly(differences);

    public IReadOnlyList<MediumApiContractDifference> Differences { get; }
    public bool IsEquivalent => Differences.Count == 0;

    public static MediumApiContractComparison Compare(MediumApiContractSnapshot expected, MediumApiContractSnapshot actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        return CompareJson(expected.ToJson(writeIndented: false), actual.ToJson(writeIndented: false));
    }

    /// <summary>Compares API Explorer description snapshots structurally, retaining nested order and all captured description values.</summary>
    public static MediumApiContractComparison CompareApiDescriptions(MediumApiDescriptionSnapshot expected, MediumApiDescriptionSnapshot actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        return CompareJson(expected.ToJson(writeIndented: false), actual.ToJson(writeIndented: false));
    }

    /// <summary>Compares arbitrary persisted contract JSON without schema validation.</summary>
    public static MediumApiContractComparison CompareJson(string expectedJson, string actualJson)
        => CompareDocuments(expectedJson, actualJson, requireObjectRoot: false);

    /// <summary>Compares OpenAPI document JSON structurally. Both roots must be objects; OpenAPI validity and schema equivalence are not inferred.</summary>
    public static MediumApiContractComparison CompareOpenApi(string expectedJson, string actualJson)
        => CompareDocuments(expectedJson, actualJson, requireObjectRoot: true);

    /// <summary>Explicit caller-controlled failure. It includes difference paths and kinds, never document values.</summary>
    public void ThrowIfDifferent()
    {
        if (IsEquivalent)
        {
            return;
        }

        var locations = Differences.Take(20).Select(difference => difference.Kind + " " + (difference.Path.Length == 0 ? "(root)" : difference.Path));
        throw new InvalidOperationException($"Contract documents differ at {Differences.Count} location(s): {string.Join("; ", locations)}");
    }

    private static MediumApiContractComparison CompareDocuments(string expectedJson, string actualJson, bool requireObjectRoot)
    {
        ArgumentNullException.ThrowIfNull(expectedJson);
        ArgumentNullException.ThrowIfNull(actualJson);
        using var expected = JsonDocument.Parse(expectedJson);
        using var actual = JsonDocument.Parse(actualJson);
        if (requireObjectRoot && (expected.RootElement.ValueKind != JsonValueKind.Object || actual.RootElement.ValueKind != JsonValueKind.Object))
        {
            throw new ArgumentException("OpenAPI JSON comparison requires an object at both document roots.");
        }

        ValidateUniquePropertyNames(expected.RootElement, string.Empty);
        ValidateUniquePropertyNames(actual.RootElement, string.Empty);
        var differences = new List<MediumApiContractDifference>();
        CompareElements(expected.RootElement, actual.RootElement, string.Empty, differences);
        return new(differences.ToArray());
    }

    private static void ValidateUniquePropertyNames(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                var propertyPath = AppendPath(path, property.Name);
                if (!names.Add(property.Name))
                {
                    throw new ArgumentException("Contract JSON contains a duplicate object property at " + propertyPath + ".");
                }

                ValidateUniquePropertyNames(property.Value, propertyPath);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                ValidateUniquePropertyNames(item, AppendPath(path, (index++).ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
        }
    }

    private static void CompareElements(JsonElement expected, JsonElement actual, string path, List<MediumApiContractDifference> differences)
    {
        if (expected.ValueKind != actual.ValueKind)
        {
            differences.Add(new(path, MediumApiContractDifferenceKind.Changed, expected.GetRawText(), actual.GetRawText()));
            return;
        }

        if (expected.ValueKind == JsonValueKind.Object)
        {
            var names = expected.EnumerateObject().Select(property => property.Name).Concat(actual.EnumerateObject().Select(property => property.Name)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
            foreach (var name in names)
            {
                var hasExpected = expected.TryGetProperty(name, out var expectedValue);
                var hasActual = actual.TryGetProperty(name, out var actualValue);
                var propertyPath = AppendPath(path, name);
                if (!hasExpected)
                {
                    differences.Add(new(propertyPath, MediumApiContractDifferenceKind.Added, null, actualValue.GetRawText()));
                }
                else if (!hasActual)
                {
                    differences.Add(new(propertyPath, MediumApiContractDifferenceKind.Removed, expectedValue.GetRawText(), null));
                }
                else
                {
                    CompareElements(expectedValue, actualValue, propertyPath, differences);
                }
            }
        }
        else if (expected.ValueKind == JsonValueKind.Array)
        {
            var expectedItems = expected.EnumerateArray().ToArray();
            var actualItems = actual.EnumerateArray().ToArray();
            for (var index = 0; index < Math.Max(expectedItems.Length, actualItems.Length); index++)
            {
                var itemPath = AppendPath(path, index.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (index >= expectedItems.Length)
                {
                    differences.Add(new(itemPath, MediumApiContractDifferenceKind.Added, null, actualItems[index].GetRawText()));
                }
                else if (index >= actualItems.Length)
                {
                    differences.Add(new(itemPath, MediumApiContractDifferenceKind.Removed, expectedItems[index].GetRawText(), null));
                }
                else
                {
                    CompareElements(expectedItems[index], actualItems[index], itemPath, differences);
                }
            }
        }
        else if (expected.ValueKind == JsonValueKind.String)
        {
            if (!string.Equals(expected.GetString(), actual.GetString(), StringComparison.Ordinal))
            {
                differences.Add(new(path, MediumApiContractDifferenceKind.Changed, expected.GetRawText(), actual.GetRawText()));
            }
        }
        else if (!string.Equals(expected.GetRawText(), actual.GetRawText(), StringComparison.Ordinal))
        {
            differences.Add(new(path, MediumApiContractDifferenceKind.Changed, expected.GetRawText(), actual.GetRawText()));
        }
    }

    private static string AppendPath(string path, string segment) => path + "/" + segment.Replace("~", "~0").Replace("/", "~1");
}