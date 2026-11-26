using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.EndpointDocumentationExample.NamedAliases;

[EndpointFilter<AliasConventionGuard>]
[NamedScope]
[Tags("Named convention aliases")]
[EndpointGroupName(DocumentationGroups.Public)]
public sealed class NamedConventionGroupDefinition : INamespaceEndpointGroup
{
    public static bool ShouldMap(EndpointMappingContext context)
    {
        return context.Configuration.GetValue("Sample:ConventionAliases", false);
    }

    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/named-conventions").WithMetadata(new AliasConventionStage("map"));
    }
}

[NamedClass]
[EndpointPattern("/value")]
public sealed partial class GetNamedConventionValueEndpoint : IGetEndpoint
{
    [NamedMethod]
    [ResponseHeader("X-Alias-Handler", "handler")]
    public Ok<AliasConventionReply> HandleAsync([FromQuery] int? page, HttpContext context, [FromServices] AliasConventionProbe probe)
    {
        return AliasConventionBodies.Create("value", page, context, probe);
    }
}

[EndpointPattern("/repeated")]
public sealed partial class GetNamedConventionRepeatedEndpoint : IGetEndpoint
{
    [NamedMethod]
    [NamedMethod]
    [NamedStruct]
    [NamedExplicitClass]
    [ResponseHeader("X-Alias-Handler", "handler")]
    public Ok<AliasConventionReply> HandleAsync([FromQuery] int? page, HttpContext context, [FromServices] AliasConventionProbe probe)
    {
        return AliasConventionBodies.Create("repeated", page, context, probe);
    }
}

[NamedClass]
public abstract class NamedConventionMethodBase : IGetEndpoint
{
    public abstract string Pattern { get; }

    [NamedMethod]
    [ResponseHeader("X-Alias-Handler", "handler")]
    public virtual Ok<AliasConventionReply> HandleAsync([FromQuery] int? page, HttpContext context, [FromServices] AliasConventionProbe probe)
    {
        return AliasConventionBodies.Create("inherited", page, context, probe);
    }
}

public sealed class GetNamedConventionInheritedEndpoint : NamedConventionMethodBase
{
    [StringSyntax("Route")]
    public override string Pattern => "/inherited";
}

public sealed class GetNamedConventionOverriddenEndpoint : NamedConventionMethodBase
{
    [StringSyntax("Route")]
    public override string Pattern => "/overridden";

    [NamedMethod]
    [ResponseHeader("X-Alias-Handler", "handler")]
    public override Ok<AliasConventionReply> HandleAsync([FromQuery] int? page, HttpContext context, [FromServices] AliasConventionProbe probe)
    {
        return AliasConventionBodies.Create("overridden", page, context, probe);
    }
}