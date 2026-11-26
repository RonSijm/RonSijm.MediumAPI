using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RonSijm.MediumAPI.CustomMetadataExample.Endpoints.Api;

[AppMetadata("ignored-group-base")]
public abstract class MetadataGroupBase { }

[AppMetadata("group")]
[EndpointFilter<ReadMetadataFilter>]
[UnmarkedThrowing]
public sealed class MetadataGroupDefinition : MetadataGroupBase, INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/metadata").WithMetadata(new AppMetadataAttribute("map"));
        group.MapGet("/native", MetadataReplies.Native).WithName("NativeMetadata");
        return group;
    }
}

[AppMetadata("class", typeof(MetadataReply), MetadataMode.Second, new[] { MetadataMode.First, MetadataMode.Second }, Aliases = new[] { "alpha", "beta" }, Extra = typeof(string), Optional = null, Flag = MetadataMode.Second)]
[UnmarkedThrowing]
public sealed class ClassMetadataEndpoint : IGetEndpoint
{
    public ClassMetadataEndpoint(MetadataProbe probe)
    {
        probe.ActivateTarget();
    }

    [StringSyntax("Route")]
    public string Pattern => "/class";

    public Ok<MetadataReply> HandleAsync(HttpContext context)
    {
        return MetadataReplies.Ok("class", context);
    }
}

public sealed class MethodMetadataEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/method";

    [AppMetadata("method")]
    [UnmarkedMethodMetadata("native-method")]
    public Ok<MetadataReply> HandleAsync(HttpContext context)
    {
        return MetadataReplies.Ok("method", context);
    }
}

[AllowAnonymous]
[EndpointFilter<ConfiguredMetadataFilter>]
public sealed class FilterMetadataEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/filter";

    public Ok<MetadataReply> HandleAsync(HttpContext context)
    {
        return MetadataReplies.Ok("filter", context);
    }
}

[AppMetadata("class")]
public sealed class OrderedMetadataEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/order";

    [AppMetadata("method")]
    public MetadataProviderResult HandleAsync()
    {
        return new MetadataProviderResult();
    }

    public static void Configure(RouteHandlerBuilder endpoint)
    {
        endpoint.WithMetadata(new AppMetadataAttribute("endpoint-configure"));
        ((IEndpointConventionBuilder)endpoint).Finally(builder => builder.Metadata.Add(new AppMetadataAttribute("endpoint-final")));
    }
}

[AppMetadata("ignored-endpoint-base")]
public abstract class MetadataEndpointBase
{
    [AppMetadata("inherited-method")]
    public Ok<MetadataReply> HandleAsync(HttpContext context)
    {
        return MetadataReplies.Ok("inherited", context);
    }
}

[AppMetadata("derived-class")]
[EndpointFilter<DerivedMetadataFilter>]
[UnmarkedThrowing]
public sealed class InheritedMetadataEndpoint : MetadataEndpointBase, IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/inherited";
}

[AppMetadata("first")]
[AppMetadata("second")]
public sealed class RepeatedMetadataEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/repeated";

    public Ok<MetadataReply> HandleAsync(HttpContext context)
    {
        return MetadataReplies.Ok("repeated", context);
    }
}

[AppMetadata("null", null, null, null, Aliases = null, Extra = null, Optional = null)]
public sealed class NullMetadataEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/null";

    public Ok<MetadataReply> HandleAsync(HttpContext context)
    {
        return MetadataReplies.Ok("null", context);
    }
}

[StaticMetadata]
public sealed class StaticMetadataEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/static";

    public Ok<MetadataReply> HandleAsync(HttpContext context)
    {
        return MetadataReplies.Ok("static", context);
    }
}

[SampleAccess(Policy = "SampleReaders")]
[AllowAnonymous]
public sealed class DualMetadataEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/dual";

    public Ok<MetadataReply> HandleAsync(HttpContext context)
    {
        return MetadataReplies.Ok("dual", context);
    }
}