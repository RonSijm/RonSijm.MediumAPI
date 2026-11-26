using System.Reflection;

namespace RonSijm.MediumAPI.CustomBindingExample.Bindings;

public sealed record RequestToken(string Value, string ParameterName) : IBindableFromHttpContext<RequestToken>
{
    public const string HeaderName = "X-Sample-Token";

    static ValueTask<RequestToken?> IBindableFromHttpContext<RequestToken>.BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var value = context.Request.Headers[HeaderName].ToString();
        RequestToken? result = string.IsNullOrEmpty(value) ? null : new RequestToken(value, parameter.Name ?? string.Empty);
        return ValueTask.FromResult(result);
    }
}