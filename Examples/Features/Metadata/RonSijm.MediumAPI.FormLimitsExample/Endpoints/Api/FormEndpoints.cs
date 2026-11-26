using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.FormLimitsExample.Endpoints.Api;

// This isolated parser sample deliberately opts out of antiforgery using the native convention.
// A browser-facing application must choose its own appropriate security policy.
[RequestFormLimits(ValueCountLimit = FormLimits.GroupCount, KeyLengthLimit = FormLimits.GroupKey, ValueLengthLimit = FormLimits.GroupValue, MultipartBodyLengthLimit = FormLimits.GroupMultipart)]
public sealed class FormGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/forms").DisableAntiforgery();
}

public sealed class PostGroupFormEndpoint(FormProbe probe) : IPostEndpoint, IDisposable
{
    private readonly FormProbe ownedProbe = Activate(probe);

    [StringSyntax("Route")]
    public string Pattern => "/group";

    public Task<Ok<FormReceipt>> HandleAsync(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("group", form, context, probe);
    public void Dispose() => ownedProbe.DisposeTarget();

    private static FormProbe Activate(FormProbe probe)
    {
        probe.Activate();
        return probe;
    }
}

[RequestFormLimits(ValueCountLimit = FormLimits.ClassCount, KeyLengthLimit = FormLimits.ClassKey, ValueLengthLimit = FormLimits.ClassValue)]
public sealed class PostClassFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class";

    public Task<Ok<FormReceipt>> HandleAsync(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("class", form, context, probe);
}

[RequestFormLimits(ValueCountLimit = FormLimits.ClassCount, KeyLengthLimit = FormLimits.ClassKey, ValueLengthLimit = FormLimits.ClassValue)]
public sealed class PostMethodFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method";

    [RequestFormLimits(ValueCountLimit = FormLimits.MethodCount, KeyLengthLimit = FormLimits.MethodKey, ValueLengthLimit = FormLimits.MethodValue)]
    public Task<Ok<FormReceipt>> HandleAsync(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("method", form, context, probe);
}

[RequestFormLimits(ValueCountLimit = FormLimits.ClassCount, KeyLengthLimit = FormLimits.ClassKey, ValueLengthLimit = FormLimits.ClassValue)]
public sealed class PostConfiguredFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/configured";

    public Task<Ok<FormReceipt>> HandleAsync(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("configured", form, context, probe);
    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithFormOptions(valueLengthLimit: FormLimits.ConfiguredValue);
}

[RequestFormLimits(ValueCountLimit = FormLimits.ClassCount, KeyLengthLimit = FormLimits.ClassKey, ValueLengthLimit = FormLimits.ClassValue)]
public sealed class PostFinalFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/final";

    public Task<Ok<FormReceipt>> HandleAsync(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("final", form, context, probe);

    public static void Configure(RouteHandlerBuilder endpoint)
    {
        endpoint.WithFormOptions(valueLengthLimit: FormLimits.ConfiguredValue);
        endpoint.Finally(builder => builder.Metadata.Add(new RequestFormLimitsAttribute { ValueCountLimit = FormLimits.ClassCount, KeyLengthLimit = FormLimits.ClassKey, ValueLengthLimit = FormLimits.FinalValue }));
    }
}

[RequestFormLimits(ValueCountLimit = FormLimits.ClassCount)]
public sealed class PostResetFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/reset";

    public Task<Ok<FormReceipt>> HandleAsync(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("reset", form, context, probe);
}

public sealed class PostSparseFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/sparse";

    public Task<Ok<FormReceipt>> HandleAsync(IFormCollection form, HttpContext context, FormProbe probe) => FormReplies.Fields("sparse", form, context, probe);
    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithFormOptions(valueLengthLimit: FormLimits.ConfiguredValue);
}

[RequestFormLimits(MultipartBodyLengthLimit = FormLimits.GroupMultipart)]
public sealed class PostFileFormEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/file";

    public Task<Ok<FormReceipt>> HandleAsync(IFormFile file, HttpContext context, FormProbe probe) => FormReplies.File("file", file, context, probe);
}