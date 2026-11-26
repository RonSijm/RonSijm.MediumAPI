namespace RonSijm.MediumAPI;

/// <summary>Declares ordered header objects at the existing owner boundary. Only list membership is copied; each child's ordinary selector snapshot is taken when its policy is registered.</summary>
/// <remarks>CLR attribute usage, inheritance and duplicate suppression apply to the set declaration. Children are header objects, not separate CLR declarations with independently applied usage rules.</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class ResponseHeadersAttribute : ResponseHeaderDeclarationAttribute
{
    /// <summary>Retains child references, subtypes, order and duplicates in a read-only membership snapshot. Empty sets are valid; null lists or children are rejected.</summary>
    protected ResponseHeadersAttribute(IReadOnlyList<ResponseHeaderAttribute> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        var headerSnapshot = headers.ToArray();
        if (Array.Exists(headerSnapshot, header => header is null)) { throw new ArgumentException("Response header declarations cannot contain null entries.", nameof(headers)); }

        Headers = Array.AsReadOnly(headerSnapshot);
    }

    /// <summary>Ordered single-header declarations. Sets cannot contain other sets; mutable child selectors retain the existing registration-time snapshot behavior.</summary>
    public IReadOnlyList<ResponseHeaderAttribute> Headers { get; }
}