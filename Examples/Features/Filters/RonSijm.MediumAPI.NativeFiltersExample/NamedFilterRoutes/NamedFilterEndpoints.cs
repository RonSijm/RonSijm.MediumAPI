using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.NativeFiltersExample.NamedFilterRoutes;

[NamedGroupGuard]
[ResponseHeader("X-Named-Group", "registered")]
public sealed class NamedFilterGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        return context.Configuration.GetValue("Sample:NamedFilterAliases", false);
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/named-filters").WithTags("Named filter aliases");
    }
}

[NamedConfiguredRead]
public sealed class GetNamedFilterValueEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/value/{value:int}";

    [NamedArgument]
    [ResponseHeader("X-Named-Handler", "entry")]
    public Ok<NamedFilterReply> HandleAsync([FromRoute] int value, [FromQuery] int? page, NamedFilterScope scope, NamedFilterProbe probe, HttpContext context)
    {
        return NamedFilterBodies.Read(value, page, scope, probe, context);
    }
}

[NamedConfiguredRead]
public sealed class GetRepeatedNamedFilterEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/repeated/{value:int}";

    [NamedArgument]
    [NamedArgument]
    [ResponseHeader("X-Named-Handler", "entry")]
    public Ok<NamedFilterReply> HandleAsync([FromRoute] int value, [FromQuery] int? page, NamedFilterScope scope, NamedFilterProbe probe, HttpContext context)
    {
        return NamedFilterBodies.Read(value, page, scope, probe, context);
    }
}

[NamedConfiguredRead]
public abstract class NamedFilterMethodBase : IGetEndpoint
{
    public abstract string Pattern { get; }

    [NamedArgument]
    [ResponseHeader("X-Named-Handler", "entry")]
    public virtual Ok<NamedFilterReply> HandleAsync([FromRoute] int value, [FromQuery] int? page, NamedFilterScope scope, NamedFilterProbe probe, HttpContext context)
    {
        return NamedFilterBodies.Read(value, page, scope, probe, context);
    }
}

public sealed class GetInheritedNamedFilterEndpoint : NamedFilterMethodBase
{
    [StringSyntax("Route")]
    public override string Pattern => "/inherited/{value:int}";
}

public sealed class GetOverriddenNamedFilterEndpoint : NamedFilterMethodBase
{
    [StringSyntax("Route")]
    public override string Pattern => "/overridden/{value:int}";

    [NamedArgument]
    [ResponseHeader("X-Named-Handler", "entry")]
    public override Ok<NamedFilterReply> HandleAsync([FromRoute] int value, [FromQuery] int? page, NamedFilterScope scope, NamedFilterProbe probe, HttpContext context)
    {
        return NamedFilterBodies.Read(value, page, scope, probe, context);
    }
}