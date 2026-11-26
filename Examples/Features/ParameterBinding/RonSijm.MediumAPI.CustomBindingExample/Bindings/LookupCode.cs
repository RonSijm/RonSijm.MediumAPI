namespace RonSijm.MediumAPI.CustomBindingExample.Bindings;

public sealed record LookupCode(string Value) : IParsable<LookupCode>
{
    static LookupCode IParsable<LookupCode>.Parse(string text, IFormatProvider? provider) => TryRead(text, out var value) ? value : throw new FormatException("A lookup code must contain 1–24 ASCII letters or digits.");

    static bool IParsable<LookupCode>.TryParse(string? text, IFormatProvider? provider, out LookupCode result) => TryRead(text, out result);

    private static bool TryRead(string? text, out LookupCode result)
    {
        if (!string.IsNullOrEmpty(text) && text.Length <= 24 && text.All(char.IsAsciiLetterOrDigit))
        {
            result = new LookupCode(text.ToLowerInvariant());
            return true;
        }

        result = null!;
        return false;
    }
}