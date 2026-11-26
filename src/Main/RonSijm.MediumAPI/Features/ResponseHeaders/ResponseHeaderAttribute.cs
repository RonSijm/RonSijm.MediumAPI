using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace RonSijm.MediumAPI;

/// <summary>Declares constant response headers on a handler, endpoint, namespace definition or registered MediumAPI filter.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ResponseHeaderAttribute : ResponseHeaderDeclarationAttribute
{
    private static readonly IReadOnlyDictionary<string, string> KnownNames = typeof(HeaderNames).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(string)).ToDictionary(field => field.Name, field => (string)field.GetValue(null)!, StringComparer.OrdinalIgnoreCase);

    private Type? resultType;
    private Type[]? resultTypes;

    /// <summary>Accepts a wire header name or framework alias such as CacheControl, and one or more constant values. Values replace existing values when this convention runs.</summary>
    public ResponseHeaderAttribute(string name, params string[] values)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(values);
        if (name.Length == 0 || name.Any(character => !IsTokenCharacter(character)))
        {
            throw new ArgumentException("A response header name must be a nonempty HTTP token.", nameof(name));
        }

        if (values.Length == 0)
        {
            throw new ArgumentException("Supply at least one response header value.", nameof(values));
        }

        foreach (var value in values)
        {
            if (value is null || value.Any(character => character < ' ' && character != '\t' || character == '\u007f'))
            {
                throw new ArgumentException("Response header values cannot contain nulls or control characters other than horizontal tabs.", nameof(values));
            }
        }

        Name = KnownNames.TryGetValue(name, out var wireName) ? wireName : name;
        Values = Array.AsReadOnly((string[])values.Clone());
    }

    /// <summary>The normalized wire name; arbitrary valid custom header names retain their supplied spelling.</summary>
    public string Name { get; }

    /// <summary>An immutable copy of the declared values.</summary>
    public IReadOnlyList<string> Values { get; }

    /// <summary>When supplied, applies after a matching IResult is returned and before it executes. Supports open generic result definitions and nested native result unions; it does not match eventual status codes.</summary>
    public Type? ResultType
    {
        get => resultType;
        set
        {
            if (value is not null && (!typeof(IResult).IsAssignableFrom(value) || value.ContainsGenericParameters && !value.IsGenericTypeDefinition))
            {
                throw new ArgumentException("A response header result selector must be an IResult type or an open generic IResult definition.", nameof(value));
            }

            resultType = value;
        }
    }

    /// <summary>When supplied, applies after any listed IResult type matches. Null leaves this selector unset; the list must be nonempty and contain no nulls. Use either ResultType or ResultTypes.</summary>
    public Type[]? ResultTypes
    {
        get { return resultTypes is null ? null : (Type[])resultTypes.Clone(); }
        set
        {
            if (value is null)
            {
                resultTypes = null;
                return;
            }

            var selectors = (Type[])value.Clone();
            if (selectors.Length == 0)
            {
                throw new ArgumentException("Supply at least one response header result selector.", nameof(value));
            }

            foreach (var selector in selectors)
            {
                if (selector is null || !typeof(IResult).IsAssignableFrom(selector) || selector.ContainsGenericParameters && !selector.IsGenericTypeDefinition)
                {
                    throw new ArgumentException("Response header result selectors must be non-null IResult types or open generic IResult definitions.", nameof(value));
                }
            }

            resultTypes = selectors;
        }
    }

    internal ResponseHeaderPolicy Snapshot()
    {
        if (resultType is not null && resultTypes is not null)
        {
            throw new InvalidOperationException("A response header declaration cannot set both ResultType and ResultTypes.");
        }

        Type[]? selectors = null;
        if (resultTypes is not null)
        {
            selectors = (Type[])resultTypes.Clone();
        }
        else if (resultType is not null)
        {
            selectors = [resultType];
        }

        return new ResponseHeaderPolicy(Name, Values.ToArray(), selectors);
    }

    private static bool IsTokenCharacter(char character) => character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' || "!#$%&'*+-.^_`|~".Contains(character);
}