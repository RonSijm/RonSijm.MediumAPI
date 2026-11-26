# Composing startup conventions

[Documentation index](README.md)

`AddMediumApiConvention<TConvention>` now accepts `IEndpointConventionBuilder`,
the builder type received by Configure. A convention can call another convention
through the same fluent API used by endpoints and groups. The existing concrete
overloads preserve their concrete return types; the interface overload returns
the same builder exposed as IEndpointConventionBuilder.

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using RonSijm.MediumAPI;

public sealed class WorkspaceAccessConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        builder.WithMetadata(new AuthorizeAttribute { Policy = SamplePolicies.Workspace });
        builder.WithMetadata(new EnableCorsAttribute(SamplePolicies.Browser));
    }
}

public sealed class WorkspaceBrowserConvention : IMediumApiEndpointConvention
{
    public static void Configure(IEndpointConventionBuilder builder)
    {
        builder.AddMediumApiFilter<BrowserTokenGuard>();
        builder.AddMediumApiConvention<WorkspaceAccessConvention>();
    }
}
```

SamplePolicies and BrowserTokenGuard are application types. The application still
owns authentication, policy definitions, token handling and middleware. Access-only
groups declare WorkspaceAccessConvention. Groups already requiring the browser
guard declare WorkspaceBrowserConvention. Sharing the access part does not add
that guard to other groups.

## Ordinary registration order

The interface overload checks for null, calls TConvention.Configure directly and
returns the original builder. It adds no execution capability beyond an ordinary
static Configure call; it completes the fluent surface for the interface the
library already supplies. Structs and explicit static interface implementations
retain their existing dispatch contract. No convention instance is constructed.

Nested calls execute immediately in call order during registration. The example
registers the guard, then authorization metadata, then CORS metadata. Native Add
and Finally callbacks still run during endpoint materialization and keep their
own ordering. Repeated calls repeat registration; application conventions should
construct fresh mutable metadata for each registration where that is required.

Only Configure executes. Attributes on outer or inner convention types are not
implicitly imported. There is no convention registry, attribute composition scan,
deduplication, policy merging or request-time layer. Existing null validation and
direct Configure exception identity remain. A failed nested call does not roll
back earlier native registrations; ordinary application call structure controls
which later operations execute.

## Preserve the complete policy boundary

Compose only an identical policy subset at its original point. Retain exact native
metadata types, field values and order. A browser convention that previously
registered a guard followed by authorization/CORS can replace only those final
two operations with the access convention. Keep response headers and other
following declarations at their existing owner and position.

Do not apply a broader browser convention to an access-only group, change a
default authorization policy to a named one, or duplicate a filter already
inherited from a parent. Native authorization and CORS precedence remain with
ASP.NET Core. CORS controls browser response access; it is not a substitute for
application authorization. See [browser policies](browser-policy-conventions.md)
and [startup conventions](startup-conventions.md).

## Runnable example and verification

The extended [native filter example](../Examples/Features/Filters/RonSijm.MediumAPI.NativeFiltersExample/)
has a sibling access-only namespace behind its own configuration flag. Its
original browser routes and checks stay. The example compares independent native
references, exact metadata, real framework cookie/CORS behavior and the absence
of browser-token checks on access-only mutations. Nested static dispatch,
registration order, exceptions, scopes and explicit aliases have focused cases.

Read the example README for the flags and routes, and the
[verification record](testing-and-development.md#current-verification-record)
for actually executed targets/checks. These synthetic hosts do not verify another
application's real identity, permissions, complete OpenAPI or deployment. Consumer
verification remains compilation only for this increment.
