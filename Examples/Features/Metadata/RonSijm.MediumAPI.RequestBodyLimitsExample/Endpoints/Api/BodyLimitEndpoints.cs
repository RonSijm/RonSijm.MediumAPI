using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.RequestBodyLimitsExample.Endpoints.Api;

[RequestSizeLimit(BodyLimits.Group)]
public sealed class BodyLimitsGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/limits");
}

public sealed class PostInheritedBodyLimitEndpoint : IPostEndpoint, IDisposable
{
    private readonly BodyLimitProbe owner;

    public PostInheritedBodyLimitEndpoint(BodyLimitProbe owner)
    {
        this.owner = owner;
        owner.RecordActivation();
    }

    [StringSyntax("Route")]
    public string Pattern => "/inherited";

    public Task<Ok<BodyLimitObservation>> HandleAsync(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("inherited", context, probe);
    public void Dispose() => owner.RecordDisposal();
}

[RequestSizeLimit(BodyLimits.Class)]
public sealed class PostClassBodyLimitEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class";

    public Task<Ok<BodyLimitObservation>> HandleAsync(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("class", context, probe);
}

[RequestSizeLimit(BodyLimits.Class)]
public sealed class PostMethodBodyLimitEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method";

    [RequestSizeLimit(BodyLimits.Method)]
    public Task<Ok<BodyLimitObservation>> HandleAsync(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("method", context, probe);
}

[RequestSizeLimit(BodyLimits.Class)]
public sealed class PostConfiguredBodyLimitEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/configured";

    [RequestSizeLimit(BodyLimits.Method)]
    public Task<Ok<BodyLimitObservation>> HandleAsync(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("configured", context, probe);

    public static void Configure(RouteHandlerBuilder endpoint) => endpoint.WithMetadata(new RequestSizeLimitAttribute(BodyLimits.Configured));
}

[RequestSizeLimit(BodyLimits.Class)]
public sealed class PostFinalBodyLimitEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/final";

    [RequestSizeLimit(BodyLimits.Method)]
    public Task<Ok<BodyLimitObservation>> HandleAsync(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("final", context, probe);

    public static void Configure(RouteHandlerBuilder endpoint)
    {
        endpoint.WithMetadata(new RequestSizeLimitAttribute(BodyLimits.Configured));
        endpoint.Finally(builder => builder.Metadata.Add(new RequestSizeLimitAttribute(BodyLimits.Final)));
    }
}

[DisableRequestSizeLimit]
public sealed class PostClassDisabledBodyLimitEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-disabled";

    public Task<Ok<BodyLimitObservation>> HandleAsync(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("class-disabled", context, probe);
}

[RequestSizeLimit(BodyLimits.Class)]
public sealed class PostMethodDisabledBodyLimitEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method-disabled";

    [DisableRequestSizeLimit]
    public Task<Ok<BodyLimitObservation>> HandleAsync(HttpContext context, BodyLimitProbe probe) => BodyLimitReader.ReadAsync("method-disabled", context, probe);
}