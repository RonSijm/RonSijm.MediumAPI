namespace RonSijm.MediumAPI.BindingMetadataExample.Bindings;

// Explicit header metadata selects native string parsing rather than this type's
// context binder. The two sources have deliberately different synthetic values.
public sealed record SelectionKey(string Value, string Source)
{
    public static bool TryParse(string? text, IFormatProvider? provider, out SelectionKey result)
    {
        if (!string.IsNullOrEmpty(text))
        {
            result = new SelectionKey(text, "header");
            return true;
        }

        result = null!;
        return false;
    }

    public static ValueTask<SelectionKey?> BindAsync(HttpContext context)
    {
        var value = context.Request.Query["binder"].ToString();
        SelectionKey? result = string.IsNullOrEmpty(value) ? null : new SelectionKey(value, "context");
        return ValueTask.FromResult(result);
    }
}