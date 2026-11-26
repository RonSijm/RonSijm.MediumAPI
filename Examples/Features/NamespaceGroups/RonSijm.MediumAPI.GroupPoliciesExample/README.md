# Namespace group policy example

This host uses class-level `[Authorize]`, `[AllowAnonymous]`, `[EnableCors]` and
`[DisableCors]` attributes on namespace group definitions. It registers ordinary
ASP.NET Core cookie authentication, a reader authorization policy, CORS policies
and their middleware. See [group policies](../../../../Docs/group-policies.md) for the
mapping and inheritance rules.

| Path | Access | CORS |
| --- | --- | --- |
| `/api/status` | Authenticated identity with `scope=sample.read` | Named browser policy |
| `/api/native-status` | Same reader policy; native `MapGet` inside the group definition | Named browser policy |
| `/api/reports/current` | Reader policy inherited from the parent group | Named browser policy |
| `/api/public/status` | Anonymous | Parent browser policy |
| `/api/public/local-only/status` | Anonymous | Disabled by the child definition |

The configured browser origin is `https://client.example`. The host deliberately
contains no login, user store or sign-in shortcut. Without an authentication
integration, protected GET requests return 401 and public GET requests return 200.
Cookie challenges and denials produce API status codes rather than redirects.
CORS does not grant an authenticated identity or block non-browser requests.

To run manually, use the repository root with either supported
target framework:

```powershell
dotnet run --project Examples/Features/NamespaceGroups/RonSijm.MediumAPI.GroupPoliciesExample --framework net8.0
```

The companion integration project uses TestServer and a synthetic authentication
handler confined to that test assembly. It exercises authorization outcomes,
namespace inheritance, anonymous subtrees, named/default CORS, preflight, native
routes, disabled groups, ignored attributes, mapping order and independent maps.
Its separate generated fixtures intentionally contain throwing constructors to
detect accidental construction of disabled or unsupported attributes.

```powershell
dotnet test Examples/Features/NamespaceGroups/RonSijm.MediumAPI.GroupPoliciesExample.IntegrationTest --framework net8.0
```

See the [verification record](../../../../Docs/testing-and-development.md) for executed
build and integration checks and remaining limitations.
