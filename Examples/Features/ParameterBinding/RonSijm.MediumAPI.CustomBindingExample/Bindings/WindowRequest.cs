using System.Reflection;

namespace RonSijm.MediumAPI.CustomBindingExample.Bindings;

public readonly record struct WindowRequest(string Value, string ParameterName, string? RouteValue)
{
    public const string HeaderName = "X-Sample-Window";

    public static ValueTask<WindowRequest?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var value = context.Request.Headers[HeaderName].ToString();
        WindowRequest? result = string.IsNullOrEmpty(value) ? null : new WindowRequest(value, parameter.Name ?? string.Empty, context.Request.RouteValues["segment"]?.ToString());
        return ValueTask.FromResult(result);
    }
}