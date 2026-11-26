using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.AntiforgeryOptOutExample;

public static class SampleAntiforgery
{
    public const string HeaderName = "X-OptOut-Antiforgery";
    public const string CookieName = "MediumApi.Sample.AntiforgeryOptOut";
    public const string FormFieldName = "__OptOutRequestVerificationToken";
}

public sealed record AntiforgeryTokenResponse(string RequestToken, string? HeaderName, string FormFieldName);

public sealed record FormReceipt(string Message, bool? ValidationSucceeded);

public sealed class FormProbe
{
    private int handlerCalls;
    private int filterCalls;

    public int HandlerCalls => Volatile.Read(ref handlerCalls);
    public int FilterCalls => Volatile.Read(ref filterCalls);

    public void HandlerInvoked() => Interlocked.Increment(ref handlerCalls);
    public void FilterInvoked() => Interlocked.Increment(ref filterCalls);
}

[DisableEndpointAntiforgery]
public sealed class OptOutFilter(FormProbe probe) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        probe.FilterInvoked();
        return next(context);
    }
}

public static class SampleReplies
{
    public static Ok<FormReceipt> Form(string message, HttpContext context, FormProbe probe)
    {
        probe.HandlerInvoked();
        return TypedResults.Ok(new FormReceipt(message, context.Features.Get<IAntiforgeryValidationFeature>()?.IsValid));
    }

    public static Ok<AntiforgeryTokenResponse> Token(HttpContext context, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        return TypedResults.Ok(new AntiforgeryTokenResponse(tokens.RequestToken!, tokens.HeaderName, tokens.FormFieldName));
    }
}