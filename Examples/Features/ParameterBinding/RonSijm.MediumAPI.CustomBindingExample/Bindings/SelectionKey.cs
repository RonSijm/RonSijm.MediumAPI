namespace RonSijm.MediumAPI.CustomBindingExample.Bindings;

// Two native mechanisms on one type demonstrate that an explicit binding source
// keeps precedence over BindAsync. No library-specific binder is involved.
public sealed record SelectionKey(string Value, string Source)
{
    public const string HeaderName = "X-Sample-Selection";

    public static bool TryParse(string? text, IFormatProvider? provider, out SelectionKey result)
    {
        if (!string.IsNullOrEmpty(text))
        {
            result = new SelectionKey(text, "query");
            return true;
        }

        result = null!;
        return false;
    }

    public static ValueTask<SelectionKey?> BindAsync(HttpContext context)
    {
        var value = context.Request.Headers[HeaderName].ToString();
        SelectionKey? result = string.IsNullOrEmpty(value) ? null : new SelectionKey(value, "context");
        return ValueTask.FromResult(result);
    }
}