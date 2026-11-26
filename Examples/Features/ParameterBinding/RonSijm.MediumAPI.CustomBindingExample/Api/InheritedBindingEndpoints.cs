using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RonSijm.MediumAPI.CustomBindingExample.Bindings;

namespace RonSijm.MediumAPI.CustomBindingExample.Api;

internal sealed class GetInheritedProductEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inherited/products/{key:long}";

    public Ok<BindingResponse> HandleAsync([FromRoute] InheritedProductKey key) => TypedResults.Ok(new BindingResponse("base-parser", key.Value.ToString(CultureInfo.InvariantCulture)));
}

internal sealed class GetGenericInheritedCodeEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inherited/codes/{key}";

    public Ok<BindingResponse> HandleAsync([FromRoute] GenericInheritedCode key) => TypedResults.Ok(new BindingResponse(key.Source, key.Value));
}

internal sealed class GetExplicitInheritedCodeEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inherited/selection";

    public Ok<BindingResponse> HandleAsync([FromQuery(Name = "selection")] GenericInheritedCode key) => TypedResults.Ok(new BindingResponse(key.Source, key.Value));
}

internal sealed class GetImplicitInheritedCodeEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inherited/selection/implicit";

    public Ok<BindingResponse> HandleAsync(GenericInheritedCode key) => TypedResults.Ok(new BindingResponse(key.Source, key.Value));
}

internal sealed class GetInheritedContextEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inherited/context/{segment}";

    public Ok<BindingResponse> HandleAsync(InheritedContextRequest request) => TypedResults.Ok(new BindingResponse(request.Source, request.Value, request.ParameterName, request.RouteValue));
}

internal sealed class GetOptionalInheritedContextEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inherited/optional-context/{segment}";

    public Ok<BindingResponse> HandleAsync(InheritedContextRequest? request) => TypedResults.Ok(request is null ? new BindingResponse("base-preferred", "none") : new BindingResponse(request.Source, request.Value, request.ParameterName, request.RouteValue));
}