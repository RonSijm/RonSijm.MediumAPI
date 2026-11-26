using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RonSijm.MediumAPI.EndpointIdentityExample.Endpoints.Api;

public sealed class IdentityGroupDefinition : INamespaceEndpointGroup
{
    public static RouteGroupBuilder Map(IEndpointRouteBuilder endpoints) => endpoints.MapGroup("/identity").AddEndpointFilterFactory(IdentityFilters.Observe);
}

public sealed class GetPlainEndpoint : IGetEndpoint, IDisposable
{
    private readonly IdentityProbe _probe;

    [StringSyntax("Route")]
    public string Pattern => "/plain/{value:int}";

    public GetPlainEndpoint(IdentityProbe probe)
    {
        _probe = probe;
        probe.RecordActivation(typeof(GetPlainEndpoint));
    }

    public Task<Ok<IdentityResponse>> HandleAsync([FromRoute] int value, IdentityProbe probe) => Task.FromResult(TypedResults.Ok(probe.RecordCall("plain", value)));
    public void Dispose() => _probe.RecordDisposal(typeof(GetPlainEndpoint));
}

// The original MethodInfo belongs to this helper, which does not implement IEndpoint.
public abstract class SharedIdentityHandlerBase
{
    public Task<Ok<IdentityResponse>> HandleAsync([FromRoute] int value, IdentityProbe probe) => Task.FromResult(TypedResults.Ok(probe.RecordCall("shared", value)));
}

public sealed class GetFirstInheritedEndpoint : SharedIdentityHandlerBase, IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/first/{value:int}";
}

public sealed class GetSecondInheritedEndpoint : SharedIdentityHandlerBase, IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/second/{value:int}";
}

public abstract class GenericIdentityHandlerBase<TCategory>
{
    public Task<Ok<IdentityResponse>> HandleAsync([FromRoute] int value, IdentityProbe probe) => Task.FromResult(TypedResults.Ok(probe.RecordCall("generic", value)));
}

public sealed class IdentityCategory { }

public sealed class GetGenericInheritedEndpoint : GenericIdentityHandlerBase<IdentityCategory>, IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/generic/{value:int}";
}

// The legacy declaring-type fallback identifies this base; the marker identifies its concrete child.
public abstract class ContractIdentityHandlerBase : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/contract-base/{value:int}";

    public Task<Ok<IdentityResponse>> HandleAsync([FromRoute] int value, IdentityProbe probe) => Task.FromResult(TypedResults.Ok(probe.RecordCall("contract", value)));
}

public sealed class GetContractInheritedEndpoint : ContractIdentityHandlerBase { }

public sealed class GetProviderEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/provider/{value:int}";

    public Task<IdentityMetadataResult> HandleAsync([FromRoute] int value, IdentityProbe probe) => Task.FromResult(new IdentityMetadataResult(probe.RecordCall("provider", value)));
}