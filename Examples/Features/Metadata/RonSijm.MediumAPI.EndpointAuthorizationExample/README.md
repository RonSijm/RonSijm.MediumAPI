# Authorization declarations on endpoint classes

This standalone example targets .NET 8 and .NET 10. It puts native `[Authorize]`
and `[AllowAnonymous]` declarations directly on MediumAPI endpoint classes. The
host registers normal cookie authentication, authorization policies and middleware.
ASP.NET Core evaluates those policies and produces the challenge, forbid or allowed
response; MediumAPI supplies the declared metadata.

```csharp
[Authorize(SamplePolicies.Readers)]
internal sealed class GetReadAuthorizationEndpoint : IGetEndpoint
{
    [StringSyntax("Route")]
    public string Pattern => "/read";

    public Ok<AuthorizationReceipt> HandleAsync(HttpContext context, AuthorizationProbe probe) => TypedResults.Ok(probe.Record("read", context.User.Identity?.IsAuthenticated == true));
}
```

The containing namespace definition supplies `/auth`. Class attributes are read
only from the actual endpoint declaration. They are added after group metadata,
before native inferred/provider and handler-method metadata. Generated
`AuthorizationPolicy` and explicit `Configure` conventions follow in their existing
order. Native requirements accumulate; a later declaration does not replace an
earlier policy merely because it is later.

## Runnable host

```shell
dotnet run --project Examples/Features/Metadata/RonSijm.MediumAPI.EndpointAuthorizationExample --framework net8.0
```

Choose `net10.0` to run the other target. The example intentionally leaves login
and identity issuance to the host application. Its companion tests add an isolated
test-only sign-in fixture with fixed synthetic principals and native `SignInAsync`;
they exercise real cookie protection and authentication rather than a custom
header-based authentication implementation.

Cookie redirects are explicitly translated to HTTP 401/403 responses in this API
sample. The host uses native ephemeral Data Protection keys so separate sample
instances have independent cookies. Restarting the sample invalidates existing
cookies; production hosts configure their own key storage and lifetime.

| GET path | Declaration and native effect |
| --- | --- |
| `/auth/open` | Unannotated control; the host's fallback policy applies if configured. |
| `/auth/default` | Class `[Authorize]` uses the host's default policy. |
| `/auth/read` | Class declaration requires the named `Readers` policy. |
| `/auth/roles` | Class declaration selects `Sample.Secondary` and permits the `Reader` or `Administrator` role. A primary-scheme cookie alone is insufficient. |
| `/auth/public-method` | Handler `[AllowAnonymous]` bypasses the class authorization requirement. |
| `/auth/property` | Class `Readers`, generated `AuthorizationPolicy` of `Writers`, and `Configure` policy `Audit` all accumulate. |
| `/auth/protected/combined` | Namespace `GroupAccess`, class `Readers` and method `Writers` all accumulate. |
| `/auth/protected/public-class` | Class `[AllowAnonymous]` bypasses group and method authorization failure handling. A method `[Authorize]` does not remove that marker. |

`AllowAnonymous` retains normal ASP.NET Core behavior. Policy construction and
authentication can still run, and invalid policy/host configuration can still
fail. It should not be interpreted as a replacement for valid host configuration.

## Verification and boundaries

The companion integration project independently maps equivalent native route
groups and handlers. It compares composed route, API Explorer and ordered
authorization metadata inventories, then exercises native 401/403/200 responses,
role and scheme selection, cumulative requirements, fallback/default policies,
invalid cookies and independent hosts. Handler counters verify rejected requests
do not invoke handlers. The probe records only fixed handler labels and counts;
cookies and principal claims are not logged by sample code.

Additional fixtures cover direct declarations, inherited handler methods,
custom authorization data, materialization order, disabled groups and explicit
aliases. Namespace ownership and existing explicit-mapping responsibilities stay
unchanged. The library does not register policies, install middleware, import CLR
base-class authorization attributes or execute MVC action filters.

See the [authorization guide](../../../../Docs/endpoint-authorization.md) and
[verification record](../../../../Docs/testing-and-development.md#current-verification-record)
for the complete contract and executed results. OpenAPI security schemes and
middleware response descriptions remain provider/host configuration.
