using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.AcceptsExample.Endpoints.Api;

public sealed class ApiGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/requests").WithTags("Request bodies");
}

// Class declarations precede native binding inference. The required JSON parameter wins here.
[Consumes(typeof(PublishRequest), "text/plain")]
public sealed class InferredJsonEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inferred";

    public Ok<BodyReply> HandleAsync([FromBody] PublishRequest request, RequestProbe probe) => RequestBodies.Json("inferred", request, probe);
}

public sealed class RequiredJsonEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/required";

    // Optional documentation does not make this non-nullable parameter optional.
    [Consumes(typeof(PublishRequest), "application/json", IsOptional = true)]
    public Ok<BodyReply> HandleAsync([FromBody] PublishRequest request, RequestProbe probe) => RequestBodies.Json("required", request, probe);
}

public sealed class NullableJsonEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/nullable";

    [Consumes(typeof(PublishRequest), "application/json", IsOptional = true)]
    public Ok<BodyReply> HandleAsync([FromBody] PublishRequest? request, RequestProbe probe) => RequestBodies.Json("nullable", request, probe);
}

public sealed class UploadEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/upload";

    // ReadFormAsync below owns parsing; this declaration does not install form binding.
    [Consumes(typeof(UploadDescription), "multipart/form-data")]
    public Task<Ok<UploadReply>> HandleAsync(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Upload(context, probe, cancellationToken);
}

public sealed class DocumentOnlyEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/documented";

    [Consumes(typeof(PublishRequest), "application/json", "application/vnd.catalog+json")]
    public Task<Ok<BodyReply>> HandleAsync(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("documented", context, probe, cancellationToken);
}

[Consumes(typeof(PublishRequest), "text/plain")]
public sealed class ClassDefaultEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/class-default";

    public Task<Ok<BodyReply>> HandleAsync(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("class-default", context, probe, cancellationToken);
}

[Consumes(typeof(PublishRequest), "text/plain")]
public sealed class MethodOverrideEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method-override";

    [Consumes(typeof(PublishRequest), "application/octet-stream")]
    public Task<Ok<BodyReply>> HandleAsync(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("method-override", context, probe, cancellationToken);
}

[EndpointFilter<UploadContractFilter>]
public sealed class FilteredUploadEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/filtered";

    public Task<Ok<UploadReply>> HandleAsync(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Upload(context, probe, cancellationToken);
}

[Consumes(typeof(PublishRequest), "text/plain")]
public sealed class LayeredEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/layered";

    [Consumes(typeof(PublishRequest), "application/json", "application/vnd.catalog+json")]
    public Task<Ok<BodyReply>> HandleAsync(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("layered", context, probe, cancellationToken);
}

public sealed class SelectJsonEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/select";

    [Consumes(typeof(PublishRequest), "application/json")]
    public Task<Ok<BodyReply>> HandleAsync(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("select-json", context, probe, cancellationToken);
}

public sealed class SelectTextEndpoint : IPostEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/select";

    [Consumes(typeof(PublishRequest), "text/plain")]
    public Task<Ok<BodyReply>> HandleAsync(HttpContext context, RequestProbe probe, CancellationToken cancellationToken) => RequestBodies.Raw("select-text", context, probe, cancellationToken);
}