using Microsoft.AspNetCore.Http;

namespace RonSijm.MediumAPI;

/// <summary>Looks up exact CLR handler parameter positions during native filter-factory construction.</summary>
public static class EndpointFilterFactoryContextExtensions
{
    /// <summary>Finds one parameter whose CLR type is exactly TArgument. Missing or ambiguous matches return false and set index to -1.</summary>
    public static bool TryGetArgumentIndex<TArgument>(this EndpointFilterFactoryContext context, out int index)
    {
        ArgumentNullException.ThrowIfNull(context);
        return TryGetArgumentIndexCore<TArgument>(context, null, out index);
    }

    /// <summary>Finds one exact CLR type and ordinal CLR parameter-name match. Binding aliases and name normalization are not used; missing or ambiguous matches return false/-1.</summary>
    public static bool TryGetArgumentIndex<TArgument>(this EndpointFilterFactoryContext context, string parameterName, out int index)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(parameterName);
        return TryGetArgumentIndexCore<TArgument>(context, parameterName, out index);
    }

    private static bool TryGetArgumentIndexCore<TArgument>(EndpointFilterFactoryContext context, string? parameterName, out int index)
    {
        index = -1;
        var matched = false;
        var argumentType = typeof(TArgument);
        foreach (var parameter in context.MethodInfo.GetParameters())
        {
            if (parameter.ParameterType != argumentType || parameterName is not null && !string.Equals(parameter.Name, parameterName, StringComparison.Ordinal))
            {
                continue;
            }

            if (matched)
            {
                index = -1;
                return false;
            }

            index = parameter.Position;
            matched = true;
        }

        return matched;
    }
}