using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace RonSijm.MediumAPI;

/// <summary>An immutable registration snapshot. It adds no response buffering, callbacks, logging or global policy.</summary>
internal sealed class ResponseHeaderPolicy(string name, string[] values, Type[]? selectedResultTypes)
{
    private readonly Type[]? resultTypes = selectedResultTypes;

    internal static async ValueTask<object?> InvokeAsync(ResponseHeaderPolicy[] policies, EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var policy in policies)
        {
            if (policy.resultTypes is null)
            {
                policy.Apply(context.HttpContext);
            }
        }

        var result = await next(context);
        if (!context.HttpContext.Response.HasStarted)
        {
            foreach (var policy in policies)
            {
                if (policy.resultTypes is not null && policy.Matches(result))
                {
                    policy.Apply(context.HttpContext);
                }
            }
        }

        return result;
    }

    private void Apply(HttpContext context) => context.Response.Headers[name] = values.Length == 1
        ? new StringValues(values[0])
        : new StringValues((string[])values.Clone());

    private bool Matches(object? result)
    {
        // Native Results<T...> can be nested. Bound traversal also leaves a custom cyclic
        // INestedHttpResult unknown rather than looping forever or executing a result.
        for (var depth = 0; result is not null && depth < 32; depth++)
        {
            foreach (var resultType in resultTypes!)
            {
                if (resultType.IsInstanceOfType(result))
                {
                    return true;
                }

                if (resultType.IsGenericTypeDefinition && MatchesGenericType(result.GetType(), resultType))
                {
                    return true;
                }
            }

            result = (result as INestedHttpResult)?.Result;
        }

        return false;
    }

    private static bool MatchesGenericType(Type type, Type resultType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == resultType)
            {
                return true;
            }
        }

        return type.GetInterfaces().Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == resultType);
    }
}