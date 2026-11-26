using System.Globalization;
using System.Reflection;

namespace RonSijm.MediumAPI.CustomBindingExample.Bindings;

public abstract class InheritedProductParser
{
    public static bool TryParse(string? text, out InheritedProductKey key)
    {
        if (long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0)
        {
            key = new InheritedProductKey(value);
            return true;
        }

        key = null!;
        return false;
    }
}

public sealed class InheritedProductKey(long value) : InheritedProductParser
{
    public long Value { get; } = value;
}

public abstract class InheritedCodeParser<T> where T : InheritedCodeParser<T>, new()
{
    public const string HeaderName = "X-Sample-Inherited-Code";
    public string Value { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;

    public static bool TryParse(string? text, IFormatProvider? provider, out T key)
    {
        if (!string.IsNullOrEmpty(text) && text.All(char.IsLetterOrDigit))
        {
            key = new T { Value = text.ToLowerInvariant(), Source = "base-provider" };
            return true;
        }

        key = null!;
        return false;
    }

    public static ValueTask<T?> BindAsync(HttpContext context)
    {
        var value = context.Request.Headers[HeaderName].ToString();
        T? key = string.IsNullOrEmpty(value) ? null : new T { Value = value, Source = "base-context" };
        return ValueTask.FromResult(key);
    }
}

public sealed class GenericInheritedCode : InheritedCodeParser<GenericInheritedCode>
{
    // Native lookup searches the preferred provider signature throughout the hierarchy
    // before considering this lower-arity declaration on the derived type.
    public static bool TryParse(string? text, out GenericInheritedCode key)
    {
        key = new GenericInheritedCode { Value = text ?? string.Empty, Source = "derived-fallback" };
        return !string.IsNullOrEmpty(text);
    }
}

public abstract class InheritedContextBinder<T> where T : InheritedContextBinder<T>, new()
{
    public const string HeaderName = "X-Sample-Inherited-Context";
    public string Value { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? ParameterName { get; set; }
    public string? RouteValue { get; set; }

    public static ValueTask<T?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var value = context.Request.Headers[HeaderName].ToString();
        T? request = string.IsNullOrEmpty(value) ? null : new T { Value = value, Source = "base-preferred", ParameterName = parameter.Name, RouteValue = context.Request.RouteValues["segment"]?.ToString() };
        return ValueTask.FromResult(request);
    }
}

public sealed class InheritedContextRequest : InheritedContextBinder<InheritedContextRequest>
{
    public static ValueTask<InheritedContextRequest?> BindAsync(HttpContext context) => ValueTask.FromResult<InheritedContextRequest?>(new() { Value = "fallback", Source = "derived-fallback" });
}