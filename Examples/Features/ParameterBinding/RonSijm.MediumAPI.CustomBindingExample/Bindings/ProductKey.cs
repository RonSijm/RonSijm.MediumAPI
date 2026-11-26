using System.Globalization;

namespace RonSijm.MediumAPI.CustomBindingExample.Bindings;

public sealed record ProductKey(long Value)
{
    public static bool TryParse(string? text, IFormatProvider? provider, out ProductKey key)
    {
        if (long.TryParse(text, NumberStyles.None, provider, out var value) && value > 0)
        {
            key = new ProductKey(value);
            return true;
        }

        key = null!;
        return false;
    }
}